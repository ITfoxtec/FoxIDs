using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Seed
{
    public class SeedLogic : LogicBase
    {
        private readonly TelemetryLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly FoxIDsControlSettings settings;
        private readonly MasterTenantDocumentsSeedLogic masterTenantDocumentsSeedLogic;
        private readonly MainTenantDocumentsSeedLogic mainTenantDocumentsSeedLogic;

        public SeedLogic(TelemetryLogger logger, IServiceProvider serviceProvider, FoxIDsControlSettings settings, MasterTenantDocumentsSeedLogic masterTenantDocumentsSeedLogic, MainTenantDocumentsSeedLogic mainTenantDocumentsSeedLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.settings = settings;
            this.masterTenantDocumentsSeedLogic = masterTenantDocumentsSeedLogic;
            this.mainTenantDocumentsSeedLogic = mainTenantDocumentsSeedLogic;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await SeedLogAsync(cancellationToken);
                await SeedDbAsync();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.CriticalError(ex);
                throw;
            }
        }

        private async Task SeedLogAsync(CancellationToken cancellationToken)
        {
            if (settings.Options?.Log == LogOptions.OpenSearchAndStdoutErrors)
            {
                try
                {
                    var openSearchTelemetryLogger = serviceProvider.GetService<OpenSearchTelemetryLogger>();
                    await openSearchTelemetryLogger.SeedAsync(cancellationToken);
                    logger.Trace("OpenSearch log storage seeded on startup.");
                }
                catch (Exception oex)
                {
                    throw new Exception("Error seeding OpenSearch log storage on startup.", oex);
                }
            }
        }

        private async Task SeedDbAsync()
        {
            if (settings.Options?.DataStorage == DataStorageOptions.MongoDb)
            {
                try
                {
                    var mongoDbRepositoryClient = serviceProvider.GetService<MongoDbRepositoryClient>();
                    await mongoDbRepositoryClient.SeedAsync();
                    logger.Trace("MongoDB storage seeded on startup.");
                }
                catch (Exception mex)
                {
                    throw new Exception("Error seeding MongoDB storage on startup.", mex);
                }
            }
            else if (settings.Options?.DataStorage == DataStorageOptions.CosmosDb)
            {
                try
                {
                    var cosmosDbDataRepositoryClient = serviceProvider.GetService<CosmosDbDataRepositoryClient>();
                    await cosmosDbDataRepositoryClient.SeedAsync();
                    logger.Trace("CosmosDB storage seeded on startup.");
                }
                catch (Exception mex)
                {
                    throw new Exception("Error seeding CosmosDB storage on startup.", mex);
                }
            }

            try
            {
                if (settings.MasterSeedEnabled)
                {
                    if (await masterTenantDocumentsSeedLogic.SeedAsync())
                    {
                        logger.Trace("Document container(s) seeded with master tenant on startup.");
                    }

                    if (settings.MainTenantSeedEnabled)
                    {
                        if (await mainTenantDocumentsSeedLogic.SeedAsync())
                        {
                            logger.Trace("Document container(s) seeded with main tenant on startup.");
                        }
                    }
                }
            }
            catch (Exception maex)
            {
                throw new Exception("Error seeding master documents on startup.", maex);
            }
        }
    }
}