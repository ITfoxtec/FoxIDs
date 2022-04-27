using System;
using System.Threading.Tasks;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using FoxIDs.SeedTool.Model;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;

namespace FoxIDs.SeedTool.Repository
{
    public class SimpleTenantRepository
    {
        private readonly SeedSettings settings;
        private CosmosClient client;
        private Container container;
        private bool isInitiated = false;

        public SimpleTenantRepository(SeedSettings settings)
        {
            this.settings = settings;
        }

        public async Task InitiateAsync()
        {
            if (!isInitiated)
            {
                isInitiated = true;
                await CreateDocumentClient();
            }
        }

        private async Task CreateDocumentClient()
        {
            Console.WriteLine("Creating Cosmos DB database and document container(s)");

            var cosmosClientBuilder = new CosmosClientBuilder(settings.CosmosDb.EndpointUri, settings.CosmosDb.PrimaryKey)
                .WithSerializerOptions(new CosmosSerializationOptions { IgnoreNullValues = true, Indented = false, PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase });
            client = cosmosClientBuilder.Build();

            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(settings.CosmosDb.DatabaseId);
            Console.WriteLine("Cosmos DB database created");

            if (settings.CosmosDb.ContainerId == settings.CosmosDb.TtlContainerId)
            {
                container = await databaseResponse.Database.CreateContainerIfNotExistsAsync(
                    new ContainerProperties
                    {
                        Id = settings.CosmosDb.TtlContainerId, 
                        PartitionKeyPath = Constants.Models.CosmosPartitionKeyPath, 
                        DefaultTimeToLive = -1 
                    });
                Console.WriteLine("One Cosmos DB document container created");
            }
            else
            {
                container = await databaseResponse.Database.CreateContainerIfNotExistsAsync(new ContainerProperties
                {
                    Id = settings.CosmosDb.ContainerId,
                    PartitionKeyPath = Constants.Models.CosmosPartitionKeyPath
                });
                _ = await databaseResponse.Database.CreateContainerIfNotExistsAsync(
                    new ContainerProperties
                    {
                        Id = settings.CosmosDb.TtlContainerId,
                        PartitionKeyPath = Constants.Models.CosmosPartitionKeyPath,
                        DefaultTimeToLive = -1
                    });
                Console.WriteLine("Two Cosmos DB document containers created");
            }
        }

        public async Task SaveAsync<T>(T item) where T : IDataDocument
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            await item.ValidateObjectAsync();

            try
            {
                await container.UpsertItemAsync(item, new PartitionKey(item.PartitionId));
            }
            catch (Exception ex)
            {
                throw new CosmosDataException(item.Id, item.PartitionId, ex);
            }
        }
    }
}
