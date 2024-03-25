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
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly MasterTenantLogic masterTenantLogic;

        public MasterTenantDocumentsSeedLogic(TelemetryLogger logger, Settings settings, ITenantDataRepository tenantDataRepository, MasterTenantLogic masterTenantLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.settings = settings;
            this.tenantDataRepository = tenantDataRepository;
            this.masterTenantLogic = masterTenantLogic;
        }

        public async Task<bool> SeedAsync()
        {
            try
            {
                if (!await CreateAndValidateMasterTenantDocumentAsync())
                {
                    return false;
                }

                await masterTenantLogic.CreateMasterTrackDocumentAsync(Constants.Routes.MasterTenantName, settings.Options.KeyStorage == KeyStorageOptions.KeyVault ? TrackKeyTypes.KeyVaultRenewSelfSigned : TrackKeyTypes.Contained);
                var mLoginUpParty = await masterTenantLogic.CreateMasterLoginDocumentAsync(Constants.Routes.MasterTenantName);
                await masterTenantLogic.CreateFirstAdminUserDocumentAsync(Constants.Routes.MasterTenantName, Constants.DefaultAdminAccount.Email, Constants.DefaultAdminAccount.Password, true, false, false, isMasterTenant: true);
                await masterTenantLogic.CreateMasterFoxIDsControlApiResourceDocumentAsync(Constants.Routes.MasterTenantName, isMasterTenant: true);
                await masterTenantLogic.CreateMasterControlClientDocmentAsync(Constants.Routes.MasterTenantName, settings.FoxIDsControlEndpoint, mLoginUpParty, includeMasterTenantScope: true);
                return true;
            }
            catch (Exception ex)
            {
                logger.CriticalError(ex, "Error seeding master tenant document.");
                throw;
            }
        }

        private async Task<bool> CreateAndValidateMasterTenantDocumentAsync()
        {
            var masterTenant = new Tenant();
            await masterTenant.SetIdAsync(new Tenant.IdKey { TenantName = Constants.Routes.MasterTenantName });

            var tenant = await tenantDataRepository.GetAsync<Tenant>(masterTenant.Id, required: false);
            if (tenant == null)
            {
                return false;
            }

            await tenantDataRepository.CreateAsync(masterTenant);
            return true;
        }
    }
}
