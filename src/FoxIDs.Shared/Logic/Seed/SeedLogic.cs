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

                    var databaseResponse = await repositoryClient.Client.CreateDatabaseIfNotExistsAsync(settings.CosmosDb.ContainerId);
                    if (databaseResponse.StatusCode == HttpStatusCode.Created)
                    {
                        if (settings.CosmosDb.ContainerId == settings.CosmosDb.TtlContainerId)
                        {
                            _ = await databaseResponse.Database.CreateContainerIfNotExistsAsync(
                                new ContainerProperties
                                {
                                    Id = settings.CosmosDb.TtlContainerId,
                                    PartitionKeyPath = Constants.Models.CosmosPartitionKeyPath,
                                    DefaultTimeToLive = -1
                                });
                            logger.Trace("One Cosmos DB Document container created.");
                        }
                        else
                        {
                            _ = await databaseResponse.Database.CreateContainerIfNotExistsAsync(new ContainerProperties
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
                            logger.Trace("Two Cosmos DB Document containers created.");
                        }

                        await masterTenantDocumentsSeedLogic.SeedAsync();
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
