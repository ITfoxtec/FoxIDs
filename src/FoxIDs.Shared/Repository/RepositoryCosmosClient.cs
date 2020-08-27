using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using System;
using Microsoft.Azure.Cosmos;

namespace FoxIDs.Repository
{
    public class RepositoryCosmosClient : IRepositoryCosmosClient, IDisposable
    {
        private bool isDisposed = false;
        private readonly TelemetryLogger logger;

        public RepositoryCosmosClient(Settings settings, TelemetryLogger logger)
        {
            this.logger = logger;
            CosmosClient = new CosmosClient(settings.CosmosDb.EndpointUri, settings.CosmosDb.PrimaryKey, new CosmosClientOptions() { AllowBulkExecution = true });
            Container = CosmosClient.GetContainer(settings.CosmosDb.DatabaseId, settings.CosmosDb.CollectionId);
        }

        public CosmosClient CosmosClient { get; private set; }
        public Container Container { get; private set; }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                try
                {
                    CosmosClient.Dispose();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error disposing CosmosDB CosmosClient.");
                }
            }
        }
    }
}
