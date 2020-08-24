using ITfoxtec.Identity;
using FoxIDs.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using System.Net;
using System.Collections.Generic;
using System.Linq.Expressions;
using Cosmos = Microsoft.Azure.Cosmos;

namespace FoxIDs.Repository
{
    public class MasterRepository : IMasterRepository
    {
        private DocumentClient client;
        private string databaseId;
        private string collectionId;
        private Uri collectionUri;
        //private DocumentCollection documentCollection;
        private Cosmos.Container container;
        private readonly TelemetryLogger logger;

        public MasterRepository(TelemetryLogger logger, IRepositoryClient repositoryClient, IRepositoryCosmosClient repositoryCosmosClient)
        {
            client = repositoryClient.Client;
            databaseId = repositoryClient.DatabaseId;
            collectionId = repositoryClient.CollectionId;
            collectionUri = repositoryClient.CollectionUri;
            //documentCollection = repositoryClient.DocumentCollection;
            container = repositoryCosmosClient.Container;
            this.logger = logger;
        }

        public async Task<bool> ExistsAsync<T>(string id) where T : MasterDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToMasterPartitionId();
            var query = GetQueryAsync<T>(partitionId).Where(d => d.Id == id).Select(d => d.Id).Take(1).AsDocumentQuery();

            double totalRU = 0;
            try
            {
                var response = await query.ExecuteNextAsync<T>();
                totalRU += response.RequestCharge;
                return response.Any();
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(id, partitionId, ex);
            }
            finally
            {
                logger.Metric($"CosmosDB RU, @master - exists id '{id}'.", totalRU);
            }
        }

        public async Task<int> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null) where T : MasterDocument
        {
            var partitionId = IdToMasterPartitionId<T>();
            var orderedQueryable = GetQueryAsync<T>(partitionId);
            var query = (whereQuery == null) ? orderedQueryable : orderedQueryable.Where(whereQuery);

            // RequestCharge not supported for count.
            //double totalRU = 0;
            try
            {
                //var response = await query.ExecuteNextAsync<T>();
                //totalRU += response.RequestCharge;
                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(partitionId, ex);
            }
            finally
            {
                //logger.Metric($"CosmosDB RU, @master - count '{typeof(T)}'.", totalRU);
            }
        }

        private string IdToMasterPartitionId<T>() where T : MasterDocument
        {
            if (typeof(T) == typeof(RiskPassword))
            {
                return RiskPassword.PartitionIdFormat(new MasterDocument.IdKey());
            }
            else
            {
                return MasterDocument.PartitionIdFormat(new MasterDocument.IdKey());
            }

        }

        public async Task<T> GetAsync<T>(string id, bool requered = true) where T : MasterDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            return await ReadDocumentAsync<T>(id, id.IdToMasterPartitionId(), requered);
        }

        //public Task<FeedResponse<TResult>> GetQueryAsync<T, TResult>(T item, Expression<Func<T, bool>> whereQuery, Expression<Func<T, TResult>> selector) where T : MasterDocument
        //{
        //    if (item == null) new ArgumentNullException(nameof(item));
        //    if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);
        //    if (whereQuery == null) new ArgumentNullException(nameof(whereQuery));
        //    if (selector == null) new ArgumentNullException(nameof(selector));

        //    return GetQueryAsync<T, TResult>(IdToPartitionId(item.Id), whereQuery, selector);
        //}
        //public async Task<FeedResponse<TResult>> GetQueryAsync<T, TResult>(string partitionId, Expression<Func<T, bool>> whereQuery, Expression<Func<T, TResult>> selector) where T : MasterDocument
        //{
        //    if (partitionId.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(partitionId));
        //    if (whereQuery == null) new ArgumentNullException(nameof(whereQuery));
        //    if (selector == null) new ArgumentNullException(nameof(selector));

        //    var query = GetQueryAsync<T>(partitionId).Where(whereQuery).Select(selector).AsDocumentQuery();

        //    double totalRU = 0;
        //    try
        //    {
        //        var response = await query.ExecuteNextAsync<TResult>();
        //        totalRU += response.RequestCharge;
        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new CosmosDataException(partitionId, ex);
        //    }
        //    finally
        //    {
        //        logger.Metric($"CosmosDB RU get query partitionId '{partitionId}'.", totalRU);
        //    }
        //}

        //public Task<int> GetQueryCountAsync<T>(T item, Expression<Func<T, bool>> whereQuery) where T : MasterDocument
        //{
        //    if (item == null) new ArgumentNullException(nameof(item));
        //    if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);
        //    if (whereQuery == null) new ArgumentNullException(nameof(whereQuery));

        //    return GetQueryCountAsync<T>(IdToPartitionId(item.Id), whereQuery);
        //}
        //public Task<int> GetQueryCountAsync<T>(string partitionId, Expression<Func<T, bool>> whereQuery) where T : MasterDocument
        //{
        //    if (partitionId.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(partitionId));
        //    if (whereQuery == null) new ArgumentNullException(nameof(whereQuery));

        //    double totalRU = 0;
        //    try
        //    {
        //        var result = GetQueryAsync<T>(partitionId).Where(whereQuery).CountAsync();
        //        // Unable to get totalRU from RequestCharge...
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new CosmosDataException(partitionId, ex);
        //    }
        //    finally
        //    {
        //        logger.Trace($"CosmosDB RU ?'{totalRU}'?, get query count type '{typeof(T)}'.", totalRU);
        //    }
        //}

        private async Task<T> ReadDocumentAsync<T>(string id, string partitionId, bool requered, bool delete = false) where T : MasterDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));
            if (partitionId.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(partitionId));

            double totalRU = 0;
            try
            {
                var documentUri = GetDocumentLink<T>(id);
                var requestOptions = new RequestOptions { PartitionKey = new PartitionKey(partitionId) };
                var item = await client.ReadDocumentAsync<T>(documentUri, requestOptions);
                totalRU += item.RequestCharge;
                if (delete)
                {
                    var deleteResponse = await client.DeleteDocumentAsync(documentUri, requestOptions);
                    totalRU += deleteResponse.RequestCharge;
                }
                if(item != null)
                {
                    await item.Document.ValidateObjectAsync();
                }
                return item;
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound && !requered)
                {
                    return default(T);
                }
                throw new CosmosDataException(id, partitionId, $"{typeof(T).Name} not found. The master seed has probably not been executed.", ex);
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(id, partitionId, ex);
            }
            finally
            {
                logger.Metric($"CosmosDB RU, @master - read document id '{id}', partitionId '{partitionId}'.", totalRU);
            }
        }

        public async Task SaveAsync<T>(T item) where T : MasterDocument
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            double totalRU = 0;
            try
            {
                var response = await client.UpsertDocumentAsync(collectionUri, item, new RequestOptions { PartitionKey = new PartitionKey(item.PartitionId) });
                totalRU += response.RequestCharge;
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(item.Id, item.PartitionId, ex);
            }
            finally
            {
                logger.Metric($"CosmosDB RU, @master - save type '{typeof(T)}'.", totalRU);
            }
        }

        public async Task DeleteAsync<T>(T item) where T : MasterDocument
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            var partitionId = item.Id.IdToMasterPartitionId();

            double totalRU = 0;
            try
            {
                var requestOptions = new RequestOptions { PartitionKey = new PartitionKey(partitionId) };
                var response = await client.DeleteDocumentAsync(GetDocumentLink<T>(item.Id), requestOptions);
                totalRU += response.RequestCharge;
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(partitionId, ex);
            }
            finally
            {
                logger.Metric($"CosmosDB RU, @master - delete id '{item.Id}', partitionId '{partitionId}'.", totalRU);
            }
        }

        public async Task SaveBulkAsync<T>(List<T> items) where T : MasterDocument
        {
            if (items?.Count <= 0) new ArgumentNullException(nameof(items));
            var firstItem = items.First();
            if (firstItem.Id.IsNullOrEmpty()) throw new ArgumentNullException($"First item {nameof(firstItem.Id)}.", items.GetType().Name);

            var partitionId = firstItem.Id.IdToMasterPartitionId();
            foreach (var item in items)
            {
                item.PartitionId = partitionId;
                item.SetDataType();
                await item.ValidateObjectAsync();
            }

            double totalRU = 0;
            try
            {
                var partitionKey = new Cosmos.PartitionKey(partitionId);
                var concurrentTasks = new List<Task>(items.Count);
                foreach (var item in items)
                {
                    concurrentTasks.Add(container.UpsertItemAsync(item, partitionKey)
                        .ContinueWith(async (responseTask) =>
                        {
                            if (responseTask.Exception != null)
                            {
                                logger.Error(responseTask.Exception);
                            }
                            totalRU += (await responseTask).RequestCharge;
                        }));
                }

                await Task.WhenAll(concurrentTasks);
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(partitionId, ex);
            }
            finally
            {
                logger.Metric($"CosmosDB RU, @master - save bulk count '{items.Count}' type '{typeof(T)}'.", totalRU);
            }
        }

        public async Task DeleteBulkAsync<T>(List<string> ids) where T : MasterDocument
        {
            if (ids?.Count <= 0) new ArgumentNullException(nameof(ids));
            var firstId = ids.First();
            if (firstId.IsNullOrEmpty()) throw new ArgumentNullException($"First id {nameof(firstId)}.", ids.GetType().Name);

            var partitionId = firstId.IdToMasterPartitionId();

            double totalRU = 0;
            try
            {
                var partitionKey = new Cosmos.PartitionKey(partitionId);
                var concurrentTasks = new List<Task>(ids.Count);
                foreach (var id in ids)
                {
                    concurrentTasks.Add(container.DeleteItemAsync<T>(id, partitionKey)
                        .ContinueWith(async (responseTask) =>
                        {
                            if (responseTask.Exception != null)
                            {
                                logger.Error(responseTask.Exception);
                            }
                            totalRU += (await responseTask).RequestCharge;
                        }));
                }

                await Task.WhenAll(concurrentTasks);
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(partitionId, ex);
            }
            finally
            {
                logger.Metric($"CosmosDB RU, @master - delete bulk count '{ids.Count}' type '{typeof(T)}'.", totalRU);
            }
        }

        //public async Task DeleteAllByTypeAsync<T>(T item) where T : MasterDocument
        //{
        //    if (item == null) new ArgumentNullException(nameof(item));
        //    if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);
        //    if (item.DataType.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.DataType), item.GetType().Name);

        //    await DeleteAllByTypeAsync<T>(item.Id.IdToMasterPartitionId(), item.DataType);
        //}

        //public async Task DeleteAllByTypeAsync<T>(string partitionId, string dataType) where T : MasterDocument
        //{
        //    if (partitionId.IsNullOrEmpty()) throw new ArgumentNullException(partitionId);
        //    if (dataType.IsNullOrEmpty()) throw new ArgumentNullException(dataType);

        //    var query = GetAllQueryAsync<T>(partitionId).Where(d => d.DataType == dataType).Select(d => d.Id).AsDocumentQuery();

        //    double totalRU = 0;
        //    try
        //    {
        //        while (query.HasMoreResults)
        //        {
        //            var result = await query.ExecuteNextAsync<string>();
        //            totalRU += result.RequestCharge;

        //            foreach (var id in result.ToList())
        //            {
        //                var requestOptions = new RequestOptions { PartitionKey = new PartitionKey(partitionId) };
        //                var deleteResponse = await client.DeleteDocumentAsync(GetDocumentLink<T>(id), requestOptions);
        //                totalRU += deleteResponse.RequestCharge;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new CosmosDataException(partitionId, ex);
        //    }
        //    finally
        //    {
        //        logger.Metric($"CosmosDB RU delete all by data type '{dataType}'.", totalRU);
        //    }
        //}

        private IOrderedQueryable<T> GetQueryAsync<T>(string partitionId, int maxItemCount = 1) where T : IDataDocument
        {
            return client.CreateDocumentQuery<T>(collectionUri, new FeedOptions() { PartitionKey = new PartitionKey(partitionId), MaxItemCount = maxItemCount });
        }

        private string PartitionIdFormat<T>(MasterDocument.IdKey idKey) where T : MasterDocument
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            if (typeof(T).Equals(typeof(RiskPassword)))
            {
                return RiskPassword.PartitionIdFormat(idKey);
            }
            else
            {
                return MasterDocument.PartitionIdFormat(idKey);
            }
        }

        private Uri GetDocumentLink<T>(string id) where T : IDataDocument
        {
            return UriFactory.CreateDocumentUri(databaseId, collectionId, id);
        }
    }
}
