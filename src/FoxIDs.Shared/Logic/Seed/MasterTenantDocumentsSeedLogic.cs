using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Seed
{
    public class MasterTenantDocumentsSeedLogic : LogicBase
    {
        private readonly TelemetryLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly Settings settings;
        private readonly ITenantDataRepository tenantDataRepository;

        public MasterTenantDocumentsSeedLogic(TelemetryLogger logger, IServiceProvider serviceProvider, Settings settings, ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.settings = settings;
            this.tenantDataRepository = tenantDataRepository;
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

                var masterTenantLogic = serviceProvider.GetService<MasterTenantLogic>();
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

        public async Task<bool> MasterTenantExist()
        {
            (bool masterTenantExist, _) = await CheckIfMasterTenantExistAsync();
            return masterTenantExist;
        }

        private async Task<bool> CreateAndValidateMasterTenantDocumentAsync()
        {
            (bool masterTenantExist, Tenant masterTenant) = await CheckIfMasterTenantExistAsync();
            if (masterTenantExist)
            {
                return false;
            }

            masterTenant.CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await tenantDataRepository.CreateAsync(masterTenant);
            return true;
        }

        private async Task<(bool masterTenantExist, Tenant masterTenant)> CheckIfMasterTenantExistAsync()
        {
            var masterTenant = new Tenant { ForUsage = false };
            await masterTenant.SetIdAsync(new Tenant.IdKey { TenantName = Constants.Routes.MasterTenantName });

            var tenant = await tenantDataRepository.GetAsync<Tenant>(masterTenant.Id, required: false);
            if (tenant != null)
            {
                return (true, masterTenant);
            }

            return (false, masterTenant);
        }

        private void SeedDataProtectorKeyData()
        {
            var dataProtection = serviceProvider.GetService<IDataProtectionProvider>();
            var initData = dataProtection.CreateProtector(nameof(MasterTenantDocumentsSeedLogic)).Protect(nameof(MasterTenantDocumentsSeedLogic));
        }
    }
}
