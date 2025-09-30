using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Seed
{
    public class MasterTenantDocumentsSeedLogic : LogicBase
    {
        private readonly TelemetryLogger logger;
        private readonly Settings settings;
        private readonly IDataProtectionProvider dataProtection;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly MasterTenantLogic masterTenantLogic;

        public MasterTenantDocumentsSeedLogic(TelemetryLogger logger, Settings settings, IDataProtectionProvider dataProtection, ITenantDataRepository tenantDataRepository, MasterTenantLogic masterTenantLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.settings = settings;
            this.dataProtection = dataProtection;
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

                SeedDataProtectorKeyData();

                await masterTenantLogic.CreateMasterTrackDocumentAsync(Constants.Routes.MasterTenantName);
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
            var masterTenant = new Tenant { ForUsage = false };
            await masterTenant.SetIdAsync(new Tenant.IdKey { TenantName = Constants.Routes.MasterTenantName });

            var tenant = await tenantDataRepository.GetAsync<Tenant>(masterTenant.Id, required: false);
            if (tenant != null)
            {
                return false;
            }

            masterTenant.CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await tenantDataRepository.CreateAsync(masterTenant);
            return true;
        }

        private void SeedDataProtectorKeyData()
        {
            var initData = dataProtection.CreateProtector(nameof(MasterTenantDocumentsSeedLogic)).Protect(nameof(MasterTenantDocumentsSeedLogic));
        }
    }
}
