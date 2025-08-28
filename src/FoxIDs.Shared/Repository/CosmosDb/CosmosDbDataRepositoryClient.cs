using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using Microsoft.Azure.Cosmos;
using System.Net;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public class CosmosDbDataRepositoryClient : CosmosDbDataRepositoryClientBase, ICosmosDbDataRepositoryClient
    {
        public CosmosDbDataRepositoryClient(Settings settings, TelemetryLogger logger) : base(settings, logger, true, false)
        {
            InitAsync().GetAwaiter().GetResult();
        }

        private async Task InitAsync()
        {
            var databaseResponse = await Client.CreateDatabaseIfNotExistsAsync(settings.CosmosDb.DatabaseId);
            if (databaseResponse.StatusCode == HttpStatusCode.Created)
            {
                if (settings.CosmosDb.ContainerId == settings.CosmosDb.TtlContainerId)
                {
                    var container = await databaseResponse.Database.CreateContainerIfNotExistsAsync(
                        new ContainerProperties
                        {
                            Id = settings.CosmosDb.TtlContainerId,
                            PartitionKeyPath = Constants.Models.CosmosPartitionKeyPath,
                            DefaultTimeToLive = -1
                        });
                    logger.Trace("One CosmosDB Document container created.");
                    SetContainers(container, container);
                }
                else
                {
                    var container = await databaseResponse.Database.CreateContainerIfNotExistsAsync(new ContainerProperties
                    {
                        Id = settings.CosmosDb.ContainerId,
                        PartitionKeyPath = Constants.Models.CosmosPartitionKeyPath
                    });
                    var ttlContainer = await databaseResponse.Database.CreateContainerIfNotExistsAsync(
                        new ContainerProperties
                        {
                            Id = settings.CosmosDb.TtlContainerId,
                            PartitionKeyPath = Constants.Models.CosmosPartitionKeyPath,
                            DefaultTimeToLive = -1
                        });
                    logger.Trace("Two CosmosDB Document containers created.");
                    SetContainers(container, ttlContainer);
                }

                logger.Trace("CosmosDB Document container(s) seeded.");
            }
        }
    }
}
