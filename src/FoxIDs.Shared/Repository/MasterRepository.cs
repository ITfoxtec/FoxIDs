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

namespace FoxIDs.Repository
{
    public class MasterRepository : IMasterRepository
    {
        private DocumentClient client;
        private string databaseId;
        private string collectionId;
        private Uri collectionUri;
        //private DocumentCollection documentCollection;
        private readonly TelemetryLogger logger;

        public MasterRepository(TelemetryLogger logger, IRepositoryClient repositoryClient)
        {
            client = repositoryClient.Client;
            databaseId = repositoryClient.DatabaseId;
            collectionId = repositoryClient.CollectionId;
            collectionUri = repositoryClient.CollectionUri;
            //documentCollection = repositoryClient.DocumentCollection;
            this.logger = logger;
        }

        public async Task<bool> ExistsAsync<T>(string id) where T : MasterDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = IdToPartitionId(id);
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
                logger.Trace($"CosmosDB RU '{totalRU}', @master - exists id '{id}'.");
            }
        }

        public async Task<T> GetAsync<T>(string id, bool requered = true) where T : MasterDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            return await ReadDocumentAsync<T>(id, IdToPartitionId(id), requered);
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
        //        logger.Trace($"CosmosDB RU '{totalRU}', get query partitionId '{partitionId}'.");
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
        //        logger.Trace($"CosmosDB RU ?'{totalRU}'?, get query count type '{typeof(T)}'.");
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
                throw new CosmosDataException(id, partitionId, ex);
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(id, partitionId, ex);
            }
            finally
            {
                logger.Trace($"CosmosDB RU '{totalRU}', @master - read document id '{id}', partitionId '{partitionId}'.");
            }
        }

        public async Task SaveAsync<T>(T item) where T : MasterDocument
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = IdToPartitionId(item.Id);
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
                logger.Trace($"CosmosDB RU '{totalRU}', @master - save type '{typeof(T)}'.");
            }
        }

        public async Task SaveBulkAsync<T>(List<T> items) where T : MasterDocument
        {
            if (items?.Count <= 0) new ArgumentNullException(nameof(items));
            var firstItem = items.First();
            if (firstItem.Id.IsNullOrEmpty()) throw new ArgumentNullException($"First item {nameof(firstItem.Id)}.", items.GetType().Name);

            var partitionId = IdToPartitionId(firstItem.Id);
            foreach(var item in items)
            {
                item.PartitionId = partitionId;
            }
            await items.ValidateObjectAsync();

            double totalRU = 0;
            try
            {
                //TODO Bulk save
                // Bulk currently not working!!! Microsoft.Azure.CosmosDB.BulkExecutor 2.3.0-preview2
                //var bulkExecutor = new BulkExecutor(client, documentCollection);
                //await bulkExecutor.InitializeAsync();

                //var response = await bulkExecutor.BulkImportAsync(
                //                documents: items,
                //                enableUpsert: true);
                //totalRU += response.TotalRequestUnitsConsumed;

                // Bulk workaround
                foreach (var item in items)
                {
                    var response = await client.UpsertDocumentAsync(collectionUri, item, new RequestOptions { PartitionKey = new PartitionKey(item.PartitionId) });
                    totalRU += response.RequestCharge;
                }
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(partitionId, ex);
            }
            finally
            {
                logger.Trace($"CosmosDB RU '{totalRU}', @master - save bulk type '{typeof(T)}'.");
            }
        }

        public async Task DeleteAsync<T>(T item) where T : MasterDocument
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            var partitionId = IdToPartitionId(item.Id);

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
                logger.Trace($"CosmosDB RU '{totalRU}', @master - delete id '{item.Id}', partitionId '{partitionId}'..");
            }
        }

        //public async Task DeleteAllByTypeAsync<T>(T item) where T : MasterDocument
        //{
        //    if (item == null) new ArgumentNullException(nameof(item));
        //    if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);
        //    if (item.DataType.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.DataType), item.GetType().Name);

        //    await DeleteAllByTypeAsync<T>(IdToPartitionId(item.Id), item.DataType);
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
        //        logger.Trace($"CosmosDB RU '{totalRU}', delete all by data type '{dataType}'.");
        //    }
        //}

        private IOrderedQueryable<T> GetQueryAsync<T>(string partitionId) where T : MasterDocument
        {
            return GetQueryAsync<T>(new FeedOptions() { MaxItemCount = 1, PartitionKey = new PartitionKey(partitionId) });
        }
        //private IOrderedQueryable<T> GetAllQueryAsync<T>(string partitionId) where T : MasterDocument
        //{
        //    return GetQueryAsync<T>(new FeedOptions() { MaxItemCount = -1, PartitionKey = new PartitionKey(partitionId) });
        //}
        private IOrderedQueryable<T> GetQueryAsync<T>(FeedOptions feedOptions) where T : MasterDocument
        {
            return client.CreateDocumentQuery<T>(collectionUri, feedOptions);
        }

        private string IdToPartitionId(string id)
        {
            var idList = id.Split(':');
            return idList[1];
        }

        private Uri GetDocumentLink<T>(string id) where T : IDataDocument
        {
            return UriFactory.CreateDocumentUri(databaseId, collectionId, id);
        }
    }
}
