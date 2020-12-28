using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Seed
{
    public class MasterTenantDocumentsSeedLogic : LogicBase
    {
        private readonly TelemetryLogger logger;
        private readonly Settings settings;
        private readonly ITenantRepository tenantRepository;
        private readonly MasterTenantLogic masterTenantLogic;

        public MasterTenantDocumentsSeedLogic(TelemetryLogger logger, Settings settings, ITenantRepository tenantRepository, MasterTenantLogic masterTenantLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.settings = settings;
            this.tenantRepository = tenantRepository;
            this.masterTenantLogic = masterTenantLogic;
        }

        public async Task SeedAsync()
        {
            try
            {
                await CreateAndValidateMasterTenantDocumentAsync();

                await masterTenantLogic.CreateMasterTrackDocumentAsync(Constants.Routes.MasterTenantName);
                var mLoginUpParty = await masterTenantLogic.CreateLoginDocumentAsync(Constants.Routes.MasterTenantName);
                await masterTenantLogic.CreateFirstAdminUserDocumentAsync(Constants.Routes.MasterTenantName, Constants.DefaultAdminAccount.Email, Constants.DefaultAdminAccount.Password, false);
                await masterTenantLogic.CreateFoxIDsControlApiResourceDocumentAsync(Constants.Routes.MasterTenantName, includeMasterTenantScope: true);
                await masterTenantLogic.CreateControlClientDocmentAsync(Constants.Routes.MasterTenantName, settings.FoxIDsControlEndpoint, mLoginUpParty, includeMasterTenantScope: true);
            }
            catch (Exception ex)
            {
                logger.CriticalError(ex, "Error seeding master tenant document.");
                throw;
            }
        }

        private async Task CreateAndValidateMasterTenantDocumentAsync()
        {
            var masterTenant = new Tenant();
            await masterTenant.SetIdAsync(new Tenant.IdKey { TenantName = Constants.Routes.MasterTenantName });

            try
            {
                _ = await tenantRepository.GetAsync<Tenant>(masterTenant.Id);
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode != HttpStatusCode.NotFound)
                {
                    throw new Exception($"{masterTenant.Id} document exists.");
                }
            }

            await tenantRepository.CreateAsync(masterTenant);
        }
    }
}
