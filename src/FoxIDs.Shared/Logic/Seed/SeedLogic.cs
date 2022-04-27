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
    public class SeedLogic : LogicBase
    {
        private readonly TelemetryLogger logger;
        private readonly Settings settings;
        private readonly IRepositoryClient repositoryClient;
        private readonly MasterTenantDocumentsSeedLogic masterTenantDocumentsSeedLogic;

        public SeedLogic(TelemetryLogger logger, Settings settings, IRepositoryClient repositoryClient, MasterTenantDocumentsSeedLogic masterTenantDocumentsSeedLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.settings = settings;
            this.repositoryClient = repositoryClient;
            this.masterTenantDocumentsSeedLogic = masterTenantDocumentsSeedLogic;
        }

        public async Task SeedAsync()
        {
            try
            {
                if (settings.MasterSeedEnabled)
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
                            (repositoryClient as RepositoryClientBase).SetContainers(container, container);
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
                            (repositoryClient as RepositoryClientBase).SetContainers(container, ttlContainer);
                        }
                        
                        await masterTenantDocumentsSeedLogic.SeedAsync();
                        logger.Trace("Cosmos DB Document container(s) seeded.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.CriticalError(ex, "Error seeding master documents.");
                throw;
            }
        }
    }
}
