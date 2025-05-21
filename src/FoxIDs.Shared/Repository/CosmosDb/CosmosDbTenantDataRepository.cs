using ITfoxtec.Identity;
using FoxIDs.Models;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Linq.Expressions;
using FoxIDs.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Data;

namespace FoxIDs.Repository
{
    public class CosmosDbTenantDataRepository : TenantDataRepositoryBase
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ICosmosDbDataRepositoryClient dataRepositoryClient;

        public CosmosDbTenantDataRepository(IHttpContextAccessor httpContextAccessor, ICosmosDbDataRepositoryClient dataRepositoryClient)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.dataRepositoryClient = dataRepositoryClient;
        }

        public override async ValueTask<bool> ExistsAsync<T>(string id, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var item = await ReadItemAsync<T>(id, id.IdToTenantPartitionId(), false, queryAdditionalIds: queryAdditionalIds, scopedLogger: scopedLogger);
            return item != null;
        }

        public override async ValueTask<long> CountAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, bool usePartitionId = true)
        {
            var partitionId = usePartitionId ? PartitionIdFormat<T>(idKey) : null;
            var orderedQueryable = GetQueryAsync<T>(partitionId, usePartitionId: usePartitionId);
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
                //logger.ScopeMetric($"CosmosDB RU, tenant - count '{typeof(T)}'.", totalRU);
            }

        }

        public override async ValueTask<T> GetAsync<T>(string id, bool required = true, bool delete = false, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            return await ReadItemAsync<T>(id, id.IdToTenantPartitionId(), required, delete: delete, queryAdditionalIds: queryAdditionalIds, scopedLogger: scopedLogger);
        }

        public override async ValueTask<Tenant> GetTenantByNameAsync(string tenantName, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            if (tenantName.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(tenantName));

            return await ReadItemAsync<Tenant>(await Tenant.IdFormatAsync(tenantName), Tenant.PartitionIdFormat(), required, scopedLogger: scopedLogger);
        }

        public override async ValueTask<Track> GetTrackByNameAsync(Track.IdKey idKey, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            return await ReadItemAsync<Track>(await Track.IdFormatAsync(idKey), Track.PartitionIdFormat(idKey), required, scopedLogger: scopedLogger);
        }

        private async ValueTask<T> ReadItemAsync<T>(string id, string partitionId, bool required, bool delete = false, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));
            if (partitionId.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(partitionId));

            double totalRU = 0;
            try
            {
                if (!queryAdditionalIds)
                {
                    var container = GetContainer<T>();
                    var partitionKey = new PartitionKey(partitionId);
                    var response = await container.ReadItemAsync<T>(id, partitionKey);
                    totalRU += response.RequestCharge;
                    if (delete)
                    {
                        var deleteResponse = await container.DeleteItemAsync<T>(id, partitionKey);
                        totalRU += deleteResponse.RequestCharge;
                    }
                    if (response != null)
                    {
                        await response.Resource.ValidateObjectAsync();
                    }
                    return response;
                }
                else
                {
                    var query = GetQueryAsync<T>(partitionId);
                    var setIterator = query.Where(q => q.Id == id || q.AdditionalIds.Contains(id)).ToFeedIterator();
                    var response = await setIterator.ReadNextAsync();
                    totalRU += response.RequestCharge;
                    var item = response.FirstOrDefault();
                    if (item == null)
                    {
                        throw new CosmosException($"Not found by {nameof(IDataDocument.AdditionalIds)}.", HttpStatusCode.NotFound, 0, null, 0);
                    }
                    if (delete)
                    {
                        var container = GetContainer<T>();
                        var deleteResponse = await container.DeleteItemAsync<T>(id, new PartitionKey(partitionId));
                        totalRU += deleteResponse.RequestCharge;
                    }
                    await item.ValidateObjectAsync();
                    return item;
                }
            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound && !required)
                {
                    return default(T);
                }
                throw new FoxIDsDataException(id, partitionId, ex);
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(id, partitionId, ex);
            }
            finally
            {
                scopedLogger = scopedLogger ?? GetScopedLogger();
                scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - read document id '{id}', partitionId '{partitionId}'."; metric.Value = totalRU; }, properties: GetProperties());
            }
        }

        public override async ValueTask<(IReadOnlyCollection<T> items, string paginationToken)> GetListAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, int pageSize = Constants.Models.ListPageSize, string paginationToken = null, TelemetryScopedLogger scopedLogger = null)
        {
            var partitionId = PartitionIdFormat<T>(idKey);
            var query = GetQueryAsync<T>(partitionId, pageSize: pageSize, continuationToken: paginationToken);
            var setIterator = (whereQuery == null) ? query.ToFeedIterator() : query.Where(whereQuery).ToFeedIterator();

            double totalRU = 0;
            try
            {
                var response = await setIterator.ReadNextAsync();
                totalRU += response.RequestCharge;
                var items = response.ToList();
                await items.ValidateObjectAsync();
                return (items, response.ContinuationToken);
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(ex);
            }
            finally
            {
                scopedLogger = scopedLogger ?? GetScopedLogger();
                scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - read list (pageSize: {pageSize}) by query of type '{typeof(T)}'."; metric.Value = totalRU; }, properties: GetProperties());
            }
        }

        public override async ValueTask CreateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            double totalRU = 0;
            try
            {
                if (item.AdditionalIds?.Count() > 0)
                {
                    foreach (var additionalId in item.AdditionalIds)
                    {
                        (var aidExist, var totalRUTemp) = await AdditionalIdExistAsync<T>(additionalId, item.PartitionId, scopedLogger: scopedLogger);
                        totalRU += totalRUTemp;
                        if (aidExist)
                        {
                            throw new FoxIDsDataException(additionalId, item.PartitionId) { StatusCode = DataStatusCode.Conflict };
                        }
                    }
                }

                var container = GetContainer(item);
                var response = await container.CreateItemAsync(item, new PartitionKey(item.PartitionId));
                totalRU += response.RequestCharge;
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
                scopedLogger = scopedLogger ?? GetScopedLogger();
                scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - create type '{typeof(T)}'."; metric.Value = totalRU; }, properties: GetProperties());
            }
        }

        public override async ValueTask UpdateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            double totalRU = 0;
            try
            {
                if (item.AdditionalIds?.Count() > 0)
                {
                    foreach (var additionalId in item.AdditionalIds)
                    {
                        (var aidExist, var totalRUTemp) = await AdditionalIdExistAsync<T>(additionalId, item.PartitionId, notId: item.Id, scopedLogger: scopedLogger);
                        totalRU += totalRUTemp;
                        if (aidExist)
                        {
                            throw new FoxIDsDataException(additionalId, item.PartitionId) { StatusCode = DataStatusCode.Conflict };
                        }
                    }
                }

                var container = GetContainer(item);
                var response = await container.ReplaceItemAsync(item, item.Id, new PartitionKey(item.PartitionId));
                totalRU += response.RequestCharge;
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
                scopedLogger = scopedLogger ?? GetScopedLogger();
                scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - update type '{typeof(T)}'."; metric.Value = totalRU; }, properties: GetProperties());
            }
        }

        public override async ValueTask SaveAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            double totalRU = 0;
            try
            {
                if (item.AdditionalIds?.Count() > 0)
                {
                    var exist = await ExistsAsync<T>(item.Id, scopedLogger: scopedLogger);
                    foreach (var additionalId in item.AdditionalIds)
                    {
                        (var aidExist, var totalRUTemp) = await AdditionalIdExistAsync<T>(additionalId, item.PartitionId, notId: exist ? item.Id : null, scopedLogger: scopedLogger);
                        totalRU += totalRUTemp;
                        if (aidExist)
                        {
                            throw new FoxIDsDataException(additionalId, item.PartitionId) { StatusCode = DataStatusCode.Conflict };
                        }
                    }
                }

                var container = GetContainer(item);
                var response = await container.UpsertItemAsync(item, new PartitionKey(item.PartitionId), new ItemRequestOptions { IndexingDirective = IndexingDirective.Exclude });
                totalRU += response.RequestCharge;
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
                scopedLogger = scopedLogger ?? GetScopedLogger();
                scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - save type '{typeof(T)}'."; metric.Value = totalRU; }, properties: GetProperties());
            }
        }

        private async Task<(bool, double totalRU)> AdditionalIdExistAsync<T>(string idOrAdditionalId, string partitionId, string notId = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            double totalRU = 0;
            var query = GetQueryAsync<T>(partitionId);
            var setIterator = query.Where(q => (notId.IsNullOrEmpty() || q.Id != notId) && (q.Id == idOrAdditionalId || q.AdditionalIds.Contains(idOrAdditionalId))).ToFeedIterator();
            var response = await setIterator.ReadNextAsync();
            totalRU += response.RequestCharge;
            var item = response.FirstOrDefault();
            return (item != null, totalRU);
        }

        public override async ValueTask SaveListAsync<T>(IReadOnlyCollection<T> items, TelemetryScopedLogger scopedLogger = null)
        {
            if (items?.Count <= 0) new ArgumentNullException(nameof(items));
            var firstItem = items.First();
            if (firstItem.Id.IsNullOrEmpty()) throw new ArgumentNullException($"First item {nameof(firstItem.Id)}.", items.GetType().Name);

            var partitionId = firstItem.Id.IdToTenantPartitionId();
            foreach (var item in items)
            {
                item.PartitionId = partitionId;
                item.SetDataType();
                await item.ValidateObjectAsync();
            }

            foreach (var item in items)
            {
                await SaveAsync(item, scopedLogger: scopedLogger);
            }
        }

        public override async ValueTask DeleteAsync<T>(string id, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToTenantPartitionId();

            if (!queryAdditionalIds)
            {
                double totalRU = 0;
                try
                {
                    var container = GetContainer<T>();
                    var deleteResponse = await container.DeleteItemAsync<T>(id, new PartitionKey(partitionId));
                    totalRU += deleteResponse.RequestCharge;
                }
                catch (Exception ex)
                {
                    throw new FoxIDsDataException(id, partitionId, ex);
                }
                finally
                {
                    scopedLogger = scopedLogger ?? GetScopedLogger();
                    scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - delete document id '{id}', partitionId '{partitionId}'."; metric.Value = totalRU; }, properties: GetProperties());
                }
            }
            else
            {
                _ = await ReadItemAsync<T>(id, partitionId, true, delete: true, queryAdditionalIds: queryAdditionalIds, scopedLogger: scopedLogger);
            }
        }

        //public override async ValueTask DeleteAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null)
        //{
        //    if (idKey == null) new ArgumentNullException(nameof(idKey));
        //    await idKey.ValidateObjectAsync();

        //    var partitionId = PartitionIdFormat<T>(idKey);
        //    var query = GetQueryAsync<T>(partitionId);
        //    var setIterator = (whereQuery == null) ? query.ToFeedIterator() : query.Where(whereQuery).ToFeedIterator();

        //    double totalRU = 0;
        //    try
        //    {
        //        var response = await setIterator.ReadNextAsync();
        //        totalRU += response.RequestCharge;         
        //        var item = response.FirstOrDefault();
        //        if (item != null)
        //        {
        //            var container = GetContainer<T>();
        //            var deleteResponse = await container.DeleteItemAsync<T>(item.Id, new PartitionKey(partitionId));
        //            totalRU += deleteResponse.RequestCharge;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new FoxIDsDataException(partitionId, ex);
        //    }
        //    finally
        //    {
        //        scopedLogger = scopedLogger ?? GetScopedLogger();
        //        scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - delete type '{typeof(T)}'."; metric.Value = totalRU; }, properties: GetProperties());
        //    }
        //}

        public override async ValueTask<long> DeleteListAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            var partitionId = PartitionIdFormat<T>(idKey);
            var query = GetQueryAsync<T>(partitionId, -1);
            var setIterator = (whereQuery == null) ? query.ToFeedIterator() : query.Where(whereQuery).ToFeedIterator();

            double totalRU = 0;
            try
            {
                var response = await setIterator.ReadNextAsync();
                totalRU += response.RequestCharge;
                var items = response.ToList();
                var count = 0;
                var container = GetContainer<T>();
                foreach (var item in items) 
                {
                    var deleteResponse = await container.DeleteItemAsync<T>(item.Id, new PartitionKey(partitionId));
                    count++;
                    totalRU += deleteResponse.RequestCharge;
                }
                return count;
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(partitionId, ex);
            }
            finally
            {
                scopedLogger = scopedLogger ?? GetScopedLogger();
                scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - delete list type '{typeof(T)}'."; metric.Value = totalRU; }, properties: GetProperties());
            }
        }

        public override async ValueTask DeleteListAsync<T>(IReadOnlyCollection<string> ids, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null)
        {
            foreach (string id in ids)
            {
                var partitionId = id.IdToTenantPartitionId();
                if (!queryAdditionalIds)
                {
                    double totalRU = 0;
                    try
                    {
                        var container = GetContainer<T>();
                        var deleteResponse = await container.DeleteItemAsync<T>(id, new PartitionKey(partitionId));
                        totalRU += deleteResponse.RequestCharge;
                    }
                    catch (CosmosException ex)
                    {
                        if (ex.StatusCode == HttpStatusCode.NotFound)
                        {
                            return;
                        }
                        throw new FoxIDsDataException(id, partitionId, ex);
                    }
                    catch (Exception ex)
                    {
                        throw new FoxIDsDataException(id, partitionId, ex);
                    }
                    finally
                    {
                        scopedLogger = scopedLogger ?? GetScopedLogger();
                        scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - delete document id '{id}', partitionId '{partitionId}'."; metric.Value = totalRU; }, properties: GetProperties());
                    }
                }
                else
                {
                    _ = await ReadItemAsync<T>(id, partitionId, false, delete: true, queryAdditionalIds: queryAdditionalIds, scopedLogger: scopedLogger);
                }
            }
        }

        private IOrderedQueryable<T> GetQueryAsync<T>(string partitionId, int pageSize = 1, string continuationToken = null, bool usePartitionId = true)
        {
            var container = GetContainer<T>();
            var queryRequestOptions = usePartitionId ? new QueryRequestOptions { PartitionKey = new PartitionKey(partitionId), MaxItemCount = pageSize } : new QueryRequestOptions { MaxItemCount = pageSize };
            return container.GetItemLinqQueryable<T>(requestOptions: queryRequestOptions, continuationToken: continuationToken);
        }

        private Container GetContainer<T>(T item = default)
        {
            if (IsTtlDocument(item))
            {
                return dataRepositoryClient.TtlContainer;
            }
            else
            {
                return dataRepositoryClient.Container;
            }
        }

        private static bool IsTtlDocument<T>(T item)
        {
            if (item != null)
            {
                return item.GetType().GetInterface(nameof(IDataTtlDocument)) != null;
            }
            else
            {
                return typeof(T).GetInterface(nameof(IDataTtlDocument)) != null;
            }
        }


        private TelemetryScopedLogger GetScopedLogger()
        {
            return httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedLogger>();
        }

        private IDictionary<string, string> GetProperties()
        {
            var routeBinding = httpContextAccessor?.HttpContext?.TryGetRouteBinding();
            if (routeBinding != null)
            {
                return new Dictionary<string, string> { { Constants.Logs.TenantName, routeBinding.TenantName }, { Constants.Logs.TrackName, routeBinding.TrackName } };
            }
            else
            {
                return null;
            }
        }
    }
}
