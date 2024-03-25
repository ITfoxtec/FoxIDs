using System;
using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;

namespace FoxIDs.Repository
{
    public abstract class CosmosDbDataRepositoryClientBase : IDisposable
    {
        private bool isDisposed = false;
        private readonly Settings settings;
        private readonly TelemetryLogger logger;
        private readonly bool withTtlContainer;
        private readonly bool withBulkExecution;

        public CosmosDbDataRepositoryClientBase(Settings settings, TelemetryLogger logger, bool withTtlContainer, bool withBulkExecution)
        {
            this.settings = settings;
            this.logger = logger;
            this.withTtlContainer = withTtlContainer;
            this.withBulkExecution = withBulkExecution;

            CreateClient();
            LoadContainers();
        }

        public CosmosClient Client { get; private set; }
        public Container Container { get; private set; }
        public Container TtlContainer { get; private set; }

        private void CreateClient()
        {
            var cosmosClientBuilder = new CosmosClientBuilder(settings.CosmosDb.EndpointUri, settings.CosmosDb.PrimaryKey)
                .WithBulkExecution(withBulkExecution)
                .WithSerializerOptions(new CosmosSerializationOptions { IgnoreNullValues = true, Indented = false, PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase });
            Client = cosmosClientBuilder.Build();
        }

        private void LoadContainers()
        {
            Container = Client.GetContainer(settings.CosmosDb.DatabaseId, settings.CosmosDb.ContainerId);
            if (withTtlContainer)
            {
                TtlContainer = Client.GetContainer(settings.CosmosDb.DatabaseId, settings.CosmosDb.TtlContainerId);
            }
        }

        public void SetContainers(Container container, Container ttlContainer)
        {
            Container = container;
            TtlContainer = ttlContainer;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                try
                {
                    Client.Dispose();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error disposing CosmosDB CosmosClient.");
                }
            }
        }
    }
}
