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

namespace FoxIDs.Repository
{
    public class TenantRepository : ITenantRepository
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IRepositoryClient repositoryClient;

        public TenantRepository(IHttpContextAccessor httpContextAccessor, IRepositoryClient repositoryClient)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.repositoryClient = repositoryClient;
        }

        public async Task<bool> ExistsAsync<T>(string id) where T : IDataDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var item = await ReadItemAsync<T>(id, id.IdToTenantPartitionId(), false);
            return item != null;
        }

        public async Task<T> GetAsync<T>(string id, bool required = true, bool delete = false) where T : IDataDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            return await ReadItemAsync<T>(id, id.IdToTenantPartitionId(), required, delete);
        }

        public async Task<Tenant> GetTenantByNameAsync(string tenantName, bool required = true)
        {
            if (tenantName.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(tenantName));

            return await ReadItemAsync<Tenant>(await Tenant.IdFormatAsync(tenantName), Tenant.PartitionIdFormat(), required);
        }

        public async Task<Track> GetTrackByNameAsync(Track.IdKey idKey, bool required = true)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            return await ReadItemAsync<Track>(await Track.IdFormatAsync(idKey), Track.PartitionIdFormat(idKey), required);
        }

        public async Task<UpParty> GetUpPartyByNameAsync(Party.IdKey idKey, bool required = true)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            return await ReadItemAsync<UpParty>(await UpParty.IdFormatAsync(idKey), DataDocument.PartitionIdFormat(idKey), required);
        }

        public async Task<DownParty> GetDownPartyByNameAsync(Party.IdKey idKey, bool required = true)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            return await ReadItemAsync<DownParty>(await DownParty.IdFormatAsync(idKey), DataDocument.PartitionIdFormat(idKey), required);
        }

        private async Task<T> ReadItemAsync<T>(string id, string partitionId, bool required, bool delete = false) where T : IDataDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));
            if (partitionId.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(partitionId));

            double totalRU = 0;
            try
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
            catch (CosmosException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound && !required)
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
                var scopedLogger = GetScopedLogger();
                scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - read document id '{id}', partitionId '{partitionId}'."; metric.Value = totalRU; }, properties: GetProperties());
            }
        }

        public async Task<HashSet<T>> GetListAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50) where T : IDataDocument
        {
            var partitionId = PartitionIdFormat<T>(idKey);
            var query = GetQueryAsync<T>(partitionId, maxItemCount: maxItemCount);
            var setIterator = (whereQuery == null) ? query.ToFeedIterator() : query.Where(whereQuery).ToFeedIterator();

            double totalRU = 0;
            try
            {
                var response = await setIterator.ReadNextAsync();
                totalRU += response.RequestCharge;
                var items = response.ToHashSet();
                await items.ValidateObjectAsync();
                return items;
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(ex);
            }
            finally
            {
                var scopedLogger = GetScopedLogger();
                scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - read list (maxItemCount: {maxItemCount}) by query of type '{typeof(T)}'."; metric.Value = totalRU; }, properties: GetProperties());
            }
        }

        public async Task CreateAsync<T>(T item) where T : IDataDocument
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            double totalRU = 0;
            try
            {
                var container = GetContainer<T>();
                var response = await container.CreateItemAsync(item, new PartitionKey(item.PartitionId));
                totalRU += response.RequestCharge;
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(item.Id, item.PartitionId, ex);
            }
            finally
            {
                var scopedLogger = GetScopedLogger();
                scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - create type '{typeof(T)}'."; metric.Value = totalRU; }, properties: GetProperties());
            }
        }

        public async Task UpdateAsync<T>(T item) where T : IDataDocument
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            double totalRU = 0;
            try
            {
                var container = GetContainer<T>();
                var response = await container.ReplaceItemAsync(item, item.Id, new PartitionKey(item.PartitionId));
                totalRU += response.RequestCharge;
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(item.Id, item.PartitionId, ex);
            }
            finally
            {
                var scopedLogger = GetScopedLogger();
                scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - update type '{typeof(T)}'."; metric.Value = totalRU; }, properties: GetProperties());
            }
        }

        public async Task SaveAsync<T>(T item) where T : IDataDocument
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            double totalRU = 0;
            try
            {
                var container = GetContainer<T>();
                var response = await container.UpsertItemAsync(item, new PartitionKey(item.PartitionId), new ItemRequestOptions { IndexingDirective = IndexingDirective.Exclude });
                totalRU += response.RequestCharge;
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(item.Id, item.PartitionId, ex);
            }
            finally
            {
                var scopedLogger = GetScopedLogger();
                scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - save type '{typeof(T)}'."; metric.Value = totalRU; }, properties: GetProperties());
            }
        }

        public async Task<T> DeleteAsync<T>(string id) where T : IDataDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToTenantPartitionId();

            double totalRU = 0;
            try
            {
                var container = GetContainer<T>();
                var deleteResponse = await container.DeleteItemAsync<T>(id, new PartitionKey(partitionId));
                totalRU += deleteResponse.RequestCharge;
                return deleteResponse;
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(id, partitionId, ex);
            }
            finally
            {
                var scopedLogger = GetScopedLogger();
                scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - delete document id '{id}', partitionId '{partitionId}'."; metric.Value = totalRU; }, properties: GetProperties());
            }
        }

        public async Task<T> DeleteAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null) where T : IDataDocument
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            var partitionId = PartitionIdFormat<T>(idKey);
            var query = GetQueryAsync<T>(partitionId);
            var setIterator = (whereQuery == null) ? query.ToFeedIterator() : query.Where(whereQuery).ToFeedIterator();

            double totalRU = 0;
            try
            {
                var response = await setIterator.ReadNextAsync();
                totalRU += response.RequestCharge;         
                var item = response.FirstOrDefault();
                if (item != null)
                {
                    var container = GetContainer<T>();
                    var deleteResponse = await container.DeleteItemAsync<T>(item.Id, new PartitionKey(partitionId));
                    totalRU += deleteResponse.RequestCharge;
                }
                await item.ValidateObjectAsync();
                return item;
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(partitionId, ex);
            }
            finally
            {
                var scopedLogger = GetScopedLogger();
                scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - delete type '{typeof(T)}'."; metric.Value = totalRU; }, properties: GetProperties());
            }
        }

        public async Task<int> DeleteListAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null) where T : IDataDocument
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
                var items = response.ToHashSet();
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
                throw new CosmosDataException(partitionId, ex);
            }
            finally
            {
                var scopedLogger = GetScopedLogger();
                scopedLogger.ScopeMetric(metric => { metric.Message = $"CosmosDB RU, tenant - delete list type '{typeof(T)}'."; metric.Value = totalRU; }, properties: GetProperties());
            }
        }

        private string PartitionIdFormat<T>(Track.IdKey idKey) where T : IDataDocument
        {
            if(typeof(T).Equals(typeof(Tenant)))
            {
                return Tenant.PartitionIdFormat();
            }
            else if (typeof(T).Equals(typeof(Track)))
            {
                if (idKey == null) new ArgumentNullException(nameof(idKey));
                return Track.PartitionIdFormat(idKey);
            }
            else
            {
                if (idKey == null) new ArgumentNullException(nameof(idKey));
                return DataDocument.PartitionIdFormat(idKey);
            }
        }

        private IOrderedQueryable<T> GetQueryAsync<T>(string partitionId, int maxItemCount = 1) where T : IDataDocument
        {
            var container = GetContainer<T>();
            return container.GetItemLinqQueryable<T>(requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(partitionId), MaxItemCount = maxItemCount });
        }

        private Container GetContainer<T>() where T : IDataDocument
        {
            if (typeof(T) is IDataTtlDocument)
            {
                return repositoryClient.TtlContainer;
            }
            else
            {
                return repositoryClient.Container;
            }
        }

        private TelemetryScopedLogger GetScopedLogger()
        {
            return httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedLogger>();
        }

        private IDictionary<string, string> GetProperties()
        {
            var routeBinding = httpContextAccessor.HttpContext.TryGetRouteBinding();
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
