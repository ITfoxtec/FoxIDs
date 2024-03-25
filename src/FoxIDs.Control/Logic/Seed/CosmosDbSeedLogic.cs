using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Azure.Cosmos;

namespace FoxIDs.Logic.Seed
{
    public class CosmosDbSeedLogic : LogicBase
    {
        private readonly TelemetryLogger logger;
        private readonly FoxIDsControlSettings settings;
        private readonly ICosmosDbDataRepositoryClient repositoryClient;

        public CosmosDbSeedLogic(TelemetryLogger logger, FoxIDsControlSettings settings, ICosmosDbDataRepositoryClient repositoryClient, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.settings = settings;
            this.repositoryClient = repositoryClient;
        }

        public async Task SeedCosmosDbAsync()
        {
            try
            {
                await settings.CosmosDb.ValidateObjectAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidConfigException("The Cosmos DB configuration is required to create the master tenant documents.", ex);
            }

            var databaseResponse = await repositoryClient.Client.CreateDatabaseIfNotExistsAsync(settings.CosmosDb.DatabaseId);
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
                    logger.Trace("One Cosmos DB Document container created.");
                    (repositoryClient as CosmosDbDataRepositoryClientBase).SetContainers(container, container);
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
                    logger.Trace("Two Cosmos DB Document containers created.");
                    (repositoryClient as CosmosDbDataRepositoryClientBase).SetContainers(container, ttlContainer);
                }
                        
                logger.Trace("Cosmos DB Document container(s) seeded.");
            }
        }
    }
}
