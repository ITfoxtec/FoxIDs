using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;

namespace FoxIDs.Logic.Seed
{
    public class SeedHostedService : IHostedService
    {
        private readonly TelemetryLogger logger;
        private readonly FoxIDsControlSettings settings;
        private readonly MasterTenantDocumentsSeedLogic masterTenantDocumentsSeedLogic;
        private readonly MainTenantDocumentsSeedLogic mainTenantDocumentsSeedLogic;

        public SeedHostedService(TelemetryLogger logger, FoxIDsControlSettings settings, MasterTenantDocumentsSeedLogic masterTenantDocumentsSeedLogic, MainTenantDocumentsSeedLogic mainTenantDocumentsSeedLogic)
        {
            this.logger = logger;
            this.settings = settings;
            this.masterTenantDocumentsSeedLogic = masterTenantDocumentsSeedLogic;
            this.mainTenantDocumentsSeedLogic = mainTenantDocumentsSeedLogic;
        }

        // Synchronous (awaited) seeding before the server starts accepting requests.
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (settings.MasterSeedEnabled)
                {
                    if (await masterTenantDocumentsSeedLogic.SeedAsync())
                    {
                        logger.Trace("Document container(s) seeded with master tenant.");
                    }

                    if (settings.MainTenantSeedEnabled)
                    {
                        if (await mainTenantDocumentsSeedLogic.SeedAsync())
                        {
                            logger.Trace("Document container(s) seeded with main tenant.");
                        }
                    }
                }
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
                logger.CriticalError(ex, "Error seeding master documents on startup.");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}