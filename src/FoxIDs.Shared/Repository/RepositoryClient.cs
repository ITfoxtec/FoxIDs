using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using Microsoft.Extensions.Hosting;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;

namespace FoxIDs.Repository
{
    public class RepositoryClient : IRepositoryClient, IDisposable
    {
        private bool isDisposed = false;
        private readonly Settings settings;
        private readonly TelemetryLogger logger;
        private readonly IHostingEnvironment environment;
        private Uri serviceEndpont;
        private Uri databaseUri;

        public RepositoryClient(Settings settings, TelemetryLogger logger, IHostingEnvironment environment)
        {
            this.settings = settings;
            this.logger = logger;
            this.environment = environment;
            DatabaseId = this.settings.CosmosDb.DatabaseId;
            CollectionId = this.settings.CosmosDb.CollectionId;
            TtlCollectionId = this.settings.CosmosDb.TtlCollectionId;
            serviceEndpont = new Uri(this.settings.CosmosDb.EndpointUri);
            databaseUri = UriFactory.CreateDatabaseUri(DatabaseId);
            CreateDocumentClient().GetAwaiter().GetResult();
        }

        public DocumentClient Client { get; private set; }
        public string DatabaseId { get; private set; }
        public string CollectionId { get; private set; }
        public string TtlCollectionId { get; private set; }
        public Uri CollectionUri => UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId);
        public Uri TtlCollectionUri => UriFactory.CreateDocumentCollectionUri(DatabaseId, TtlCollectionId);
        public DocumentCollection DocumentCollection { get; private set; }
        public DocumentCollection TtlDocumentCollection { get; private set; }

        private async Task CreateDocumentClient()
        {
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
            };
            jsonSerializerSettings.Converters.Add(new StringEnumConverter(typeof(CamelCaseNamingStrategy)));

            Client = new DocumentClient(serviceEndpont, settings.CosmosDb.PrimaryKey,
                jsonSerializerSettings,
                new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp
                });

            if (environment.IsDevelopment())
            {
                await Client.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseId });

                var partitionKeyDefinition = new PartitionKeyDefinition { Paths = new Collection<string> { "/partition_id" } };
                var documentCollection = new DocumentCollection { Id = CollectionId, PartitionKey = partitionKeyDefinition };
                var ttlDocumentCollection = new DocumentCollection { Id = TtlCollectionId, PartitionKey = partitionKeyDefinition, DefaultTimeToLive = -1 };
                if (CollectionId == TtlCollectionId)
                {
                    DocumentCollection = TtlDocumentCollection = await Client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, ttlDocumentCollection);
                    logger.Trace("One Cosmos DB Document Collection created.");
                }
                else
                {
                    DocumentCollection = await Client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, documentCollection);
                    TtlDocumentCollection = await Client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, ttlDocumentCollection);
                    logger.Trace("Two Cosmos DB Document Collections created.");
                }
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                try
                {
                    (Client as DocumentClient).Dispose();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error disposing CosmosDB DocumentClient.");
                }
            }
        }
    }
}
