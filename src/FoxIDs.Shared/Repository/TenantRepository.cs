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
        private readonly TelemetryLogger logger;

        public TenantRepository(TelemetryLogger logger, IRepositoryClient repositoryClient)
        {
            client = repositoryClient.Client;
            databaseId = repositoryClient.DatabaseId;
            collectionId = repositoryClient.CollectionId;
            ttlCollectionId = repositoryClient.TtlCollectionId;
            collectionUri = repositoryClient.CollectionUri;
            ttlCollectionUri = repositoryClient.TtlCollectionUri;
            this.logger = logger;
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
                logger.Trace($"CosmosDB RU '{totalRU}', tenant - exists id '{id}'.");
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

            return await ReadDocumentAsync<Tenant>(Tenant.IdFormat(new Tenant.IdKey { TenantName = tenantName }), Tenant.PartitionIdFormat(tenantName), requered);
        }

        public async Task<Track> GetTrackByNameAsync(Track.IdKey idKey, bool requered = true)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return await ReadDocumentAsync<Track>(Track.IdFormat(idKey), DataDocument.PartitionIdFormat(idKey), requered);
        }

        public async Task<UpParty> GetUpPartyByNameAsync(Party.IdKey idKey, bool requered = true)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return await ReadDocumentAsync<UpParty>(UpParty.IdFormat(idKey), DataDocument.PartitionIdFormat(idKey), requered);
        }

        public async Task<DownParty> GetDownPartyByNameAsync(Party.IdKey idKey, bool requered = true)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return await ReadDocumentAsync<DownParty>(DownParty.IdFormat(idKey), DataDocument.PartitionIdFormat(idKey), requered);
        }

        private async Task<T> ReadDocumentAsync<T>(string id, string partitionId, bool requered, bool delete = false) where T : IDataDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));
            if (partitionId.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(partitionId));

            double totalRU = 0;
            try
            {
                var documentUri = UriFactory.CreateDocumentUri(databaseId, GetCollectionId<T>(), id);
                var requestOptions = new RequestOptions { PartitionKey = new PartitionKey(partitionId) };
                var response = await client.ReadDocumentAsync<T>(documentUri, requestOptions);
                totalRU += response.RequestCharge;
                if (delete)
                {
                    var deleteResponse = await client.DeleteDocumentAsync(documentUri, requestOptions);
                    totalRU += deleteResponse.RequestCharge;
                }
                await response?.Document?.ValidateObjectAsync();
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
                logger.Trace($"CosmosDB RU '{totalRU}', tenant - read document id '{id}', partitionId '{partitionId}'.");
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
                logger.Trace($"CosmosDB RU '{totalRU}', tenant - create type '{typeof(T)}'.");
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
                logger.Trace($"CosmosDB RU '{totalRU}', tenant - save type '{typeof(T)}'.");
            }
        }

        public async Task DeleteAsync<T>(string id) where T : IDataDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToPartitionId();

            double totalRU = 0;
            try
            {
                var documentUri = UriFactory.CreateDocumentUri(databaseId, GetCollectionId<T>(), id);
                var requestOptions = new RequestOptions { PartitionKey = new PartitionKey(partitionId) };
                var deleteResponse = await client.DeleteDocumentAsync(documentUri, requestOptions);
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
                logger.Trace($"CosmosDB RU '{totalRU}', tenant - delete document id '{id}', partitionId '{partitionId}'.");
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
                    var documentUri = UriFactory.CreateDocumentUri(databaseId, GetCollectionId<T>(), item.Id);
                    var requestOptions = new RequestOptions { PartitionKey = new PartitionKey(partitionId) };
                    var deleteResponse = await client.DeleteDocumentAsync(documentUri, requestOptions);
                    totalRU += deleteResponse.RequestCharge;
                }
                await item?.ValidateObjectAsync();
                return item;
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(partitionId, ex);
            }
            finally
            {
                logger.Trace($"CosmosDB RU '{totalRU}', tenant - delete type '{typeof(T)}'.");
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
