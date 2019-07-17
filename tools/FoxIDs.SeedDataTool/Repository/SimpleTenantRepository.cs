using FoxIDs.Models.Config;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;

namespace FoxIDs.SeedDataTool.Repository
{
    public class SimpleTenantRepository
    {
        private readonly Settings settings;
        private DocumentClient client;
        private Uri collectionUri;

        public SimpleTenantRepository(Settings settings)
        {
            this.settings = settings;
            collectionUri = UriFactory.CreateDocumentCollectionUri(settings.CosmosDb.DatabaseId, settings.CosmosDb.CollectionId);
            CreateDocumentClient().GetAwaiter().GetResult();
        }

        private async Task CreateDocumentClient()
        {
            Console.WriteLine("Creating Cosmos DB database and document collection(s)");

            client = new DocumentClient(new Uri(settings.CosmosDb.EndpointUri), settings.CosmosDb.PrimaryKey,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                },
                new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp
                });

            await client.CreateDatabaseIfNotExistsAsync(new Database { Id = settings.CosmosDb.DatabaseId });
            Console.WriteLine("Cosmos DB database created");

            var partitionKeyDefinition = new PartitionKeyDefinition { Paths = new Collection<string> { "/partition_id" } };
            var documentCollection = new DocumentCollection { Id = settings.CosmosDb.CollectionId, PartitionKey = partitionKeyDefinition };
            var ttlDocumentCollection = new DocumentCollection { Id = settings.CosmosDb.TtlCollectionId, PartitionKey = partitionKeyDefinition, DefaultTimeToLive = -1 };

            var databaseUri = UriFactory.CreateDatabaseUri(settings.CosmosDb.DatabaseId);
            if (settings.CosmosDb.CollectionId == settings.CosmosDb.TtlCollectionId)
            {
                await client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, ttlDocumentCollection);
                Console.WriteLine("One Cosmos DB document collection created");
            }
            else
            {
                await client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, documentCollection);
                await client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, ttlDocumentCollection);
                Console.WriteLine("Two Cosmos DB document collections created");
            }
        }

        public async Task SaveAsync<T>(T item) where T : IDataDocument
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            await item.ValidateObjectAsync();

            try
            {
                await client.UpsertDocumentAsync(collectionUri, item, new RequestOptions { PartitionKey = new PartitionKey(item.PartitionId) });
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(item.Id, item.PartitionId, ex);
            }
        }
    }
}
