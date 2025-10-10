using ITfoxtec.Identity;
using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace FoxIDs.Repository
{
    public class CosmosDbMasterDataRepository : MasterDataRepositoryBase
    {
        private readonly Container container;
        private readonly Container bulkContainer;
        private readonly TelemetryLogger logger;
        private readonly AuditLogic auditLogic;

        public CosmosDbMasterDataRepository(TelemetryLogger logger, ICosmosDbDataRepositoryClient dataRepositoryClient, ICosmosDbDataRepositoryBulkClient dataRepositoryBulkClient, AuditLogic auditLogic)
        {
            container = dataRepositoryClient.Container;
            bulkContainer = dataRepositoryBulkClient.Container;
            this.logger = logger;
            this.auditLogic = auditLogic;
        }

        public override async ValueTask<bool> ExistsAsync<T>(string id)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToMasterPartitionId();
            var query = GetQueryAsync<T>(partitionId).Where(d => d.Id == id);

            // RequestCharge not supported for count.
            //double totalRU = 0;
            try
            {
                //var response = await query.ExecuteNextAsync<T>();
                //totalRU += response.RequestCharge;
                return (await query.CountAsync()) > 0;
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(id, partitionId, ex);
            }
            finally
            {
                //logger.Metric($"CosmosDB RU, @master - exists id '{id}'.", totalRU);
            }
        }

        public override async ValueTask<long> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null)
        {
            var partitionId = TypeToMasterPartitionId<T>();
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
                throw new FoxIDsDataException(partitionId, ex);
            }
            finally
            {
                //logger.Metric($"CosmosDB RU, @master - count '{typeof(T)}'.", totalRU);
            }
        }

        public override async ValueTask<T> GetAsync<T>(string id, bool required = true)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            return await ReadItemAsync<T>(id, id.IdToMasterPartitionId(), required);
        }        

        private async ValueTask<T> ReadItemAsync<T>(string id, string partitionId, bool required, bool delete = false)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));
            if (partitionId.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(partitionId));

            double totalRU = 0;
            try
            {
                var partitionKey = new PartitionKey(partitionId);
                var item = await container.ReadItemAsync<T>(id, partitionKey);
                totalRU += item.RequestCharge;
                if (delete)
                {
                    var deleteResponse = await container.DeleteItemAsync<T>(id, partitionKey);
                    totalRU += deleteResponse.RequestCharge;
                }
                if(item != null)
                {
                    await item.Resource.ValidateObjectAsync();
                }
                return item;
            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound && !required)
                {
                    return default(T);
                }
                throw new FoxIDsDataException(id, partitionId, $"{typeof(T).Name} not found. The master seed has probably not been executed.", ex);
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(id, partitionId, ex);
            }
            finally
            {
                logger.Metric($"CosmosDB RU, @master - read document id '{id}', partitionId '{partitionId}'.", totalRU);
            }
        }

        public override async ValueTask<IReadOnlyCollection<T>> GetManyAsync<T>(Expression<Func<T, bool>> whereQuery = null, int pageSize = Constants.Models.ListPageSize)
        {
            var partitionId = TypeToMasterPartitionId<T>();
            var query = GetQueryAsync<T>(partitionId, pageSize: pageSize);
            var setIterator = (whereQuery == null) ? query.ToFeedIterator() : query.Where(whereQuery).ToFeedIterator();

            double totalRU = 0;
            try
            {
                var response = await setIterator.ReadNextAsync();
                totalRU += response.RequestCharge;
                var items = response.ToList();
                await items.ValidateObjectAsync();
                return items;
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(ex);
            }
            finally
            {
                logger.Metric($"CosmosDB RU, @master - read list (pageSize: {pageSize}) by query of type '{typeof(T)}', partitionId '{partitionId}'.", totalRU);
            }
        }

        public override async ValueTask CreateAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            double totalRU = 0;
            var shouldAudit = auditLogic.ShouldLogAuditData();
            try
            {
                var response = await container.CreateItemAsync(item, new PartitionKey(item.PartitionId));
                totalRU += response.RequestCharge;

                if (shouldAudit)
                {
                    auditLogic.LogDataEvent(AuditDataActions.Create, default, item, item.Id);
                }
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(item.Id, item.PartitionId, ex);
            }
            finally
            {
                logger.Metric($"CosmosDB RU, @master - create type '{typeof(T)}'.", totalRU);
            }
        }

        public override async ValueTask UpdateAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            double totalRU = 0;
            var shouldAudit = auditLogic.ShouldLogAuditData();
            T existing = default;
            try
            {
                if (shouldAudit)
                {
                    existing = await ReadItemAsync<T>(item.Id, item.PartitionId, required: true);
                }

                var response = await container.ReplaceItemAsync(item, item.Id, new PartitionKey(item.PartitionId));
                totalRU += response.RequestCharge;

                if (shouldAudit)
                {
                    auditLogic.LogDataEvent(AuditDataActions.Update, existing, item, item.Id);
                }
            }
            catch (FoxIDsDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(item.Id, item.PartitionId, ex);
            }
            finally
            {
                logger.Metric($"CosmosDB RU, @master - update type '{typeof(T)}'.", totalRU);
            }
        }

        public override async ValueTask SaveAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            double totalRU = 0;
            var shouldAudit = auditLogic.ShouldLogAuditData();
            T existing = default;
            try
            {
                if (shouldAudit)
                {
                    existing = await ReadItemAsync<T>(item.Id, item.PartitionId, required: false);
                }

                var response = await container.UpsertItemAsync(item, new PartitionKey(item.PartitionId));
                totalRU += response.RequestCharge;

                if (shouldAudit)
                {
                    auditLogic.LogDataEvent(AuditDataActions.Save, existing, item, item.Id);
                }
            }
            catch (FoxIDsDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(item.Id, item.PartitionId, ex);
            }
            finally
            {
                logger.Metric($"CosmosDB RU, @master - save type '{typeof(T)}'.", totalRU);
            }
        }

        public override async ValueTask DeleteAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            var partitionId = item.Id.IdToMasterPartitionId();

            double totalRU = 0;
            var shouldAudit = auditLogic.ShouldLogAuditData();
            try
            {
                var response = await container.DeleteItemAsync<T>(item.Id, new PartitionKey(partitionId));
                totalRU += response.RequestCharge;

                if (shouldAudit)
                {
                    auditLogic.LogDataEvent(AuditDataActions.Delete, item, default, item.Id);
                }
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(partitionId, ex);
            }
            finally
            {
                logger.Metric($"CosmosDB RU, @master - delete id '{item.Id}', partitionId '{partitionId}'.", totalRU);
            }
        }

        public override async ValueTask SaveManyAsync<T>(IReadOnlyCollection<T> items)
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
            var shouldAudit = auditLogic.ShouldLogAuditData();
            Dictionary<string, T> existingLookup = null;
            try
            {
                if (shouldAudit)
                {
                    existingLookup = new Dictionary<string, T>(items.Count);
                    foreach (var item in items)
                    {
                        try
                        {
                            var existingItem = await ReadItemAsync<T>(item.Id, partitionId, required: false);
                            if (existingItem != null)
                            {
                                existingLookup[item.Id] = existingItem;
                            }
                        }
                        catch (FoxIDsDataException)
                        {
                            throw;
                        }
                    }
                }

                var partitionKey = new PartitionKey(partitionId);
                var concurrentTasks = new List<Task>(items.Count);
                foreach (var item in items)
                {
                    concurrentTasks.Add(bulkContainer.UpsertItemAsync(item, partitionKey)
                        .ContinueWith(async (responseTask) =>
                        {
                            if (!responseTask.IsCompletedSuccessfully)
                            {
                                var innerException = responseTask.Exception.Flatten()?.InnerExceptions?.FirstOrDefault();
                                if (innerException != null)
                                {
                                    throw new FoxIDsDataException(partitionId, innerException);
                                }
                                else
                                {
                                    throw new FoxIDsDataException(partitionId);
                                }
                            }
                            totalRU += (await responseTask).RequestCharge;
                        }));
                }

                await Task.WhenAll(concurrentTasks);

                if (shouldAudit)
                {
                    foreach (var item in items)
                    {
                        T existing = default;
                        if (existingLookup != null && existingLookup.TryGetValue(item.Id, out var existingValue))
                        {
                            existing = existingValue;
                        }
                        auditLogic.LogDataEvent(AuditDataActions.Save, existing, item, item.Id);
                    }
                }
            }
            catch (FoxIDsDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(partitionId, ex);
            }
            finally
            {
                logger.Metric($"CosmosDB RU, @master - save bulk count '{items.Count}' type '{typeof(T)}'.", totalRU);
            }
        }

        public override async ValueTask DeleteAsync<T>(string id)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToMasterPartitionId();

            double totalRU = 0;
            var shouldAudit = auditLogic.ShouldLogAuditData();
            T existing = default;
            try
            {
                if (shouldAudit)
                {
                    existing = await ReadItemAsync<T>(id, partitionId, required: true);
                }

                var deleteResponse = await container.DeleteItemAsync<T>(id, new PartitionKey(partitionId));
                totalRU += deleteResponse.RequestCharge;

                if (shouldAudit)
                {
                    auditLogic.LogDataEvent(AuditDataActions.Delete, existing, default, id);
                }
            }
            catch (FoxIDsDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(id, partitionId, ex);
            }
            finally
            {
                logger.Metric($"CosmosDB RU, @master - delete document id '{id}', partitionId '{partitionId}'.", totalRU);
            }
        }

        public override async ValueTask DeleteManyAsync<T>(IReadOnlyCollection<string> ids)
        {
            if (ids?.Count <= 0) new ArgumentNullException(nameof(ids));
            var firstId = ids.First();
            if (firstId.IsNullOrEmpty()) throw new ArgumentNullException($"First id {nameof(firstId)}.", ids.GetType().Name);

            var partitionId = firstId.IdToMasterPartitionId();

            double totalRU = 0;
            var shouldAudit = auditLogic.ShouldLogAuditData();
            Dictionary<string, T> existingLookup = null;
            try
            {
                if (shouldAudit)
                {
                    existingLookup = new Dictionary<string, T>(ids.Count);
                    foreach (var id in ids)
                    {
                        try
                        {
                            var existingItem = await ReadItemAsync<T>(id, partitionId, required: false);
                            if (existingItem != null)
                            {
                                existingLookup[id] = existingItem;
                            }
                        }
                        catch (FoxIDsDataException)
                        {
                            throw;
                        }
                    }
                }

                var partitionKey = new PartitionKey(partitionId);
                var concurrentTasks = new List<Task>(ids.Count);
                foreach (var id in ids)
                {
                    concurrentTasks.Add(bulkContainer.DeleteItemAsync<T>(id, partitionKey)
                        .ContinueWith(async (responseTask) =>
                        {
                            if (!responseTask.IsCompletedSuccessfully)
                            {
                                var innerException = responseTask.Exception.Flatten()?.InnerExceptions?.FirstOrDefault();
                                if (innerException != null)
                                {
                                    throw new FoxIDsDataException(partitionId, innerException);
                                }
                                else
                                {
                                    throw new FoxIDsDataException(partitionId);
                                }
                            }
                            totalRU += (await responseTask).RequestCharge;
                        }));
                }

                await Task.WhenAll(concurrentTasks);

                if (shouldAudit && existingLookup?.Count > 0)
                {
                    foreach (var existing in existingLookup.Values)
                    {
                        auditLogic.LogDataEvent(AuditDataActions.Delete, existing, default, existing.Id);
                    }
                }
            }
            catch (FoxIDsDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(partitionId, ex);
            }
            finally
            {
                logger.Metric($"CosmosDB RU, @master - delete bulk count '{ids.Count}' type '{typeof(T)}'.", totalRU);
            }
        }

        public override ValueTask DeleteManyAsync<T>()
        {
            throw new NotSupportedException("Not supported by CosmosDB.");
        }

        private IOrderedQueryable<T> GetQueryAsync<T>(string partitionId, int pageSize = 1) where T : IDataDocument
        {           
            return container.GetItemLinqQueryable<T>(requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(partitionId), MaxItemCount = pageSize });
        }
    }
}
