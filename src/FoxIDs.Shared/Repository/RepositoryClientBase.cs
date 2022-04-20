using System;
using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;

namespace FoxIDs.Repository
{
    public abstract class RepositoryClientBase : IDisposable
    {
        private bool isDisposed = false;
        private readonly Settings settings;
        private readonly TelemetryLogger logger;

        public RepositoryClientBase(Settings settings, TelemetryLogger logger, bool withTtlContainer, bool withBulkExecution)
        {
            this.settings = settings;
            this.logger = logger;

            CreateClientAndContainers(withTtlContainer, withBulkExecution);
        }

        public CosmosClient Client { get; private set; }
        public Container Container { get; private set; }
        public Container TtlContainer { get; private set; }

        private void CreateClientAndContainers(bool withTtlContainer, bool withBulkExecution)
        {
            var cosmosClientBuilder = new CosmosClientBuilder(settings.CosmosDb.EndpointUri, settings.CosmosDb.PrimaryKey)
                .WithBulkExecution(withBulkExecution)
                .WithSerializerOptions(new CosmosSerializationOptions { IgnoreNullValues = true, Indented = false, PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase });
            Client = cosmosClientBuilder.Build();

            Container = Client.GetContainer(settings.CosmosDb.DatabaseId, settings.CosmosDb.ContainerId);
            if (withTtlContainer)
            {
                TtlContainer = Client.GetContainer(settings.CosmosDb.DatabaseId, settings.CosmosDb.TtlContainerId);
            }
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
