using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace FoxIDs.Logic.Seed
{
    public class SeedLogic : LogicBase
    {
        private static SemaphoreSlim signal = new SemaphoreSlim(1, 1);
        private static bool isSeeded = false;
        private readonly TelemetryLogger logger;
        private readonly FoxIDsControlSettings settings;
        private readonly MasterTenantDocumentsSeedLogic masterTenantDocumentsSeedLogic;
        private readonly MainTenantDocumentsSeedLogic mainTenantDocumentsSeedLogic;

        public SeedLogic(TelemetryLogger logger, FoxIDsControlSettings settings, MasterTenantDocumentsSeedLogic masterTenantDocumentsSeedLogic, MainTenantDocumentsSeedLogic mainTenantDocumentsSeedLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.settings = settings;
            this.masterTenantDocumentsSeedLogic = masterTenantDocumentsSeedLogic;
            this.mainTenantDocumentsSeedLogic = mainTenantDocumentsSeedLogic;
        }

        public async Task SeedAsync()
        {
            if (isSeeded)
            {
                return;
            }
            await signal.WaitAsync();
            try
            {
                if (!isSeeded)
                {
                    isSeeded = true;
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
            }
            catch (Exception ex)
            {
                logger.CriticalError(ex, "Error seeding master documents.");
                throw;
            }
            finally
            {
                signal.Release(1);
            }
        }
    }
}
