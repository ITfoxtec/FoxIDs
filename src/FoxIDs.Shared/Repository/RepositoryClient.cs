using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using Newtonsoft.Json.Converters;

namespace FoxIDs.Repository
{
    public class RepositoryClient : IRepositoryClient, IDisposable
    {
        private bool isDisposed = false;
        private readonly Settings settings;
        private readonly TelemetryLogger logger;
        private readonly IServiceProvider serviceProvider;
        private Uri serviceEndpont;

        public RepositoryClient(Settings settings, TelemetryLogger logger, IServiceProvider serviceProvider)
        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            DatabaseId = this.settings.CosmosDb.DatabaseId;
            CollectionId = this.settings.CosmosDb.CollectionId;
            TtlCollectionId = this.settings.CosmosDb.TtlCollectionId;
            serviceEndpont = new Uri(this.settings.CosmosDb.EndpointUri);
            DatabaseUri = UriFactory.CreateDatabaseUri(DatabaseId);
            CreateDocumentClient();
        }

        public DocumentClient Client { get; private set; }
        public string DatabaseId { get; private set; }
        public string CollectionId { get; private set; }
        public string TtlCollectionId { get; private set; }
        public Uri DatabaseUri { get; private set; }
        public Uri CollectionUri => UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId);
        public Uri TtlCollectionUri => UriFactory.CreateDocumentCollectionUri(DatabaseId, TtlCollectionId);

        private void CreateDocumentClient()
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
