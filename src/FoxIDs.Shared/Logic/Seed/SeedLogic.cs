using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using System.Collections.ObjectModel;
using System.Net;

namespace FoxIDs.Logic.Seed
{
    public class SeedLogic : LogicBase
    {
        private readonly TelemetryLogger logger;
        private readonly Settings settings;
        private readonly IRepositoryClient repositoryClient;
        private readonly ResourceSeedLogic resourceSeedLogic;
        private readonly MasterTenantDocumentsSeedLogic masterTenantDocumentsSeedLogic;

        public SeedLogic(TelemetryLogger logger, Settings settings, IRepositoryClient repositoryClient, ResourceSeedLogic resourceSeedLogic, MasterTenantDocumentsSeedLogic masterTenantDocumentsSeedLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.settings = settings;
            this.repositoryClient = repositoryClient;
            this.resourceSeedLogic = resourceSeedLogic;
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

                    var db = await repositoryClient.Client.CreateDatabaseIfNotExistsAsync(new Database { Id = repositoryClient.DatabaseId });
                    if (db.StatusCode == HttpStatusCode.Created)
                    {
                        var partitionKeyDefinition = new PartitionKeyDefinition { Paths = new Collection<string> { "/partition_id" } };
                        var documentCollection = new DocumentCollection { Id = repositoryClient.CollectionId, PartitionKey = partitionKeyDefinition };
                        var ttlDocumentCollection = new DocumentCollection { Id = repositoryClient.TtlCollectionId, PartitionKey = partitionKeyDefinition, DefaultTimeToLive = -1 };
                        if (repositoryClient.CollectionId == repositoryClient.TtlCollectionId)
                        {
                            _ = await repositoryClient.Client.CreateDocumentCollectionIfNotExistsAsync(repositoryClient.DatabaseUri, ttlDocumentCollection);
                            logger.Trace("One Cosmos DB Document Collection created.");
                        }
                        else
                        {
                            _ = await repositoryClient.Client.CreateDocumentCollectionIfNotExistsAsync(repositoryClient.DatabaseUri, documentCollection);
                            _ = await repositoryClient.Client.CreateDocumentCollectionIfNotExistsAsync(repositoryClient.DatabaseUri, ttlDocumentCollection);
                            logger.Trace("Two Cosmos DB Document Collections created.");
                        }

                        await resourceSeedLogic.SeedAsync();
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
