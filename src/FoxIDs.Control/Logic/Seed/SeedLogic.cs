using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Logic.Seed
{
    public class SeedLogic : LogicBase
    {
        private static bool isSeeded = false;
        private readonly TelemetryLogger logger;
        private readonly FoxIDsControlSettings settings;
        private readonly IServiceProvider serviceProvider;
        private readonly MasterTenantDocumentsSeedLogic masterTenantDocumentsSeedLogic;

        public SeedLogic(TelemetryLogger logger, FoxIDsControlSettings settings, IServiceProvider serviceProvider, MasterTenantDocumentsSeedLogic masterTenantDocumentsSeedLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.settings = settings;
            this.serviceProvider = serviceProvider;
            this.masterTenantDocumentsSeedLogic = masterTenantDocumentsSeedLogic;
        }

        public async Task SeedAsync()
        {
            try
            {
                if (!isSeeded && settings.MasterSeedEnabled)
                {
                    isSeeded = true;
                    if (settings.Options.DataStorage == DataStorageOptions.CosmosDb)
                    {
                        await serviceProvider.GetService<CosmosDbSeedLogic>().SeedCosmosDbAsync();
                    }

                    if (await masterTenantDocumentsSeedLogic.SeedAsync())
                    {
                        logger.Trace("Document container(s) seeded.");
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
