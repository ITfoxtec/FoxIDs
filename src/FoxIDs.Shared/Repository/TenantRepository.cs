using ITfoxtec.Identity;
using FoxIDs.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Linq.Expressions;
using FoxIDs.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Repository
{
    public class TenantRepository : ITenantRepository
    {
        private DocumentClient client;
        private string databaseId;
        private string collectionId;
        private string ttlCollectionId;
        private Uri collectionUri;
        private Uri ttlCollectionUri;
        private readonly IHttpContextAccessor httpContextAccessor;

        public TenantRepository(IHttpContextAccessor httpContextAccessor, IRepositoryClient repositoryClient)
        {
            client = repositoryClient.Client;
            databaseId = repositoryClient.DatabaseId;
            collectionId = repositoryClient.CollectionId;
            ttlCollectionId = repositoryClient.TtlCollectionId;
            collectionUri = repositoryClient.CollectionUri;
            ttlCollectionUri = repositoryClient.TtlCollectionUri;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> ExistsAsync<T>(string id) where T : IDataDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToPartitionId();
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
                var scopedLogger = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedLogger>();
                scopedLogger.ScopeMetric($"CosmosDB RU, tenant - exists id '{id}'.", totalRU);
            }
        }

        public async Task<T> GetAsync<T>(string id, bool requered = true, bool delete = false) where T : IDataDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            return await ReadDocumentAsync<T>(id, id.IdToPartitionId(), requered, delete);
        }

        public async Task<Tenant> GetTenantByNameAsync(string tenantName, bool requered = true)
        {
            if (tenantName.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(tenantName));

            return await ReadDocumentAsync<Tenant>(await Tenant.IdFormat(new Tenant.IdKey { TenantName = tenantName }), Tenant.PartitionIdFormat(tenantName), requered);
        }

        public async Task<Track> GetTrackByNameAsync(Track.IdKey idKey, bool requered = true)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            return await ReadDocumentAsync<Track>(await Track.IdFormat(idKey), DataDocument.PartitionIdFormat(idKey), requered);
        }

        public async Task<UpParty> GetUpPartyByNameAsync(Party.IdKey idKey, bool requered = true)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            return await ReadDocumentAsync<UpParty>(await UpParty.IdFormat(idKey), DataDocument.PartitionIdFormat(idKey), requered);
        }

        public async Task<DownParty> GetDownPartyByNameAsync(Party.IdKey idKey, bool requered = true)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            return await ReadDocumentAsync<DownParty>(await DownParty.IdFormat(idKey), DataDocument.PartitionIdFormat(idKey), requered);
        }

        private async Task<T> ReadDocumentAsync<T>(string id, string partitionId, bool requered, bool delete = false) where T : IDataDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));
            if (partitionId.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(partitionId));

            double totalRU = 0;
            try
            {
                var documentLink = GetDocumentLink<T>(id);
                var requestOptions = new RequestOptions { PartitionKey = new PartitionKey(partitionId) };
                var response = await client.ReadDocumentAsync<T>(documentLink, requestOptions);
                totalRU += response.RequestCharge;
                if (delete)
                {
                    var deleteResponse = await client.DeleteDocumentAsync(documentLink, requestOptions);
                    totalRU += deleteResponse.RequestCharge;
                }
                if (response != null)
                {
                    await response.Document.ValidateObjectAsync(); 
                }
                return response;
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
                var scopedLogger = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedLogger>();
                scopedLogger.ScopeMetric($"CosmosDB RU, tenant - read document id '{id}', partitionId '{partitionId}'.", totalRU);
            }
        }
        
        public async Task CreateAsync<T>(T item) where T : IDataDocument
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetPartitionId();
            await item.ValidateObjectAsync();

            double totalRU = 0;
            try
            {
                var response = await client.CreateDocumentAsync(GetCollectionLink<T>(), item, new RequestOptions { PartitionKey = new PartitionKey(item.PartitionId) });
                totalRU += response.RequestCharge;
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(item.Id, item.PartitionId, ex);
            }
            finally
            {
                var scopedLogger = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedLogger>();
                scopedLogger.ScopeMetric($"CosmosDB RU, tenant - create type '{typeof(T)}'.", totalRU);
            }
        }

        public async Task UpdateAsync<T>(T item) where T : IDataDocument
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetPartitionId();
            await item.ValidateObjectAsync();

            double totalRU = 0;
            try
            {
                var response = await client.ReplaceDocumentAsync(GetDocumentLink<T>(item.Id), item, new RequestOptions { PartitionKey = new PartitionKey(item.PartitionId) });
                totalRU += response.RequestCharge;
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(item.Id, item.PartitionId, ex);
            }
            finally
            {
                var scopedLogger = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedLogger>();
                scopedLogger.ScopeMetric($"CosmosDB RU, tenant - update type '{typeof(T)}'.", totalRU);
            }
        }

        public async Task SaveAsync<T>(T item) where T : IDataDocument
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetPartitionId();
            await item.ValidateObjectAsync();

            double totalRU = 0;
            try
            {
                var response = await client.UpsertDocumentAsync(GetCollectionLink<T>(), item, new RequestOptions { PartitionKey = new PartitionKey(item.PartitionId) });
                totalRU += response.RequestCharge;
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(item.Id, item.PartitionId, ex);
            }
            finally
            {
                var scopedLogger = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedLogger>();
                scopedLogger.ScopeMetric($"CosmosDB RU, tenant - save type '{typeof(T)}'.", totalRU);
            }
        }

        public async Task DeleteAsync<T>(string id) where T : IDataDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToPartitionId();

            double totalRU = 0;
            try
            {
                var requestOptions = new RequestOptions { PartitionKey = new PartitionKey(partitionId) };
                var deleteResponse = await client.DeleteDocumentAsync(GetDocumentLink<T>(id), requestOptions);
                totalRU += deleteResponse.RequestCharge;
            }
            catch (DocumentClientException ex)
            {
                throw new CosmosDataException(id, partitionId, ex);
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(id, partitionId, ex);
            }
            finally
            {
                var scopedLogger = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedLogger>();
                scopedLogger.ScopeMetric($"CosmosDB RU, tenant - delete document id '{id}', partitionId '{partitionId}'.", totalRU);
            }
        }

        public async Task<T> DeleteAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery) where T : IDataDocument
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            var partitionId = DataDocument.PartitionIdFormat(idKey);
            var query = GetQueryAsync<T>(partitionId).Where(whereQuery).AsDocumentQuery();

            double totalRU = 0;
            try
            {
                var response = await query.ExecuteNextAsync<T>();
                totalRU += response.RequestCharge;
                var item = response.FirstOrDefault();
                if (item != null)
                {
                    var requestOptions = new RequestOptions { PartitionKey = new PartitionKey(partitionId) };
                    var deleteResponse = await client.DeleteDocumentAsync(GetDocumentLink<T>(item.Id), requestOptions);
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
                var scopedLogger = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedLogger>();
                scopedLogger.ScopeMetric($"CosmosDB RU, tenant - delete type '{typeof(T)}'.", totalRU);
            }
        }

        private IOrderedQueryable<T> GetQueryAsync<T>(string partitionId) where T : IDataDocument
        {
            return client.CreateDocumentQuery<T>(GetCollectionLink<T>(), new FeedOptions() { MaxItemCount = 1, PartitionKey = new PartitionKey(partitionId) });
        }

        private string GetCollectionId<T>() where T : IDataDocument
        {
            if (typeof(T) is IDataTtlDocument)
            {
                return ttlCollectionId;
            }
            else
            {
                return collectionId;
            }
        }

        private Uri GetDocumentLink<T>(string id) where T : IDataDocument
        {
            return UriFactory.CreateDocumentUri(databaseId, GetCollectionId<T>(), id);
        }

        private Uri GetCollectionLink<T>() where T : IDataDocument
        {
            if (typeof(T) is IDataTtlDocument)
            {
                return ttlCollectionUri;
            }
            else
            {
                return collectionUri;
            }
        }
    }
}
