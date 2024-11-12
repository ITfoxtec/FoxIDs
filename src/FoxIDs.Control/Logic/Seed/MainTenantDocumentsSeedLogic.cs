using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Seed
{
    public class MainTenantDocumentsSeedLogic : LogicBase 
    {
        private readonly TelemetryLogger logger;
        private readonly Settings settings;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly MasterTenantLogic masterTenantLogic;

        public MainTenantDocumentsSeedLogic(TelemetryLogger logger, Settings settings, ITenantDataRepository tenantDataRepository, MasterTenantLogic masterTenantLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
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
                if (!await CreateAndValidateMainTenantDocumentAsync(settings.FoxIDsEndpoint))
                {
                    return false;
                }

                await masterTenantLogic.CreateMasterTrackDocumentAsync(Constants.Routes.MainTenantName);
                var mLoginUpParty = await masterTenantLogic.CreateMasterLoginDocumentAsync(Constants.Routes.MainTenantName);
                await masterTenantLogic.CreateFirstAdminUserDocumentAsync(Constants.Routes.MainTenantName, Constants.DefaultAdminAccount.Email, Constants.DefaultAdminAccount.Password, true, false, false, isMasterTenant: true);
                await masterTenantLogic.CreateMasterFoxIDsControlApiResourceDocumentAsync(Constants.Routes.MainTenantName, isMasterTenant: true);
                await masterTenantLogic.CreateMasterControlClientDocmentAsync(Constants.Routes.MainTenantName, settings.FoxIDsControlEndpoint, mLoginUpParty, includeMasterTenantScope: true);

                await masterTenantLogic.CreateDefaultTracksDocmentsAsync(Constants.Routes.MainTenantName);
                return true;
            }
            catch (Exception ex)
            {
                logger.CriticalError(ex, "Error seeding master tenant document.");
                throw;
            }
        }

        private async Task<bool> CreateAndValidateMainTenantDocumentAsync(string foxIDsEndpoint)
        {
            var mainTenant = new Tenant();
            await mainTenant.SetIdAsync(new Tenant.IdKey { TenantName = Constants.Routes.MainTenantName });

            var tenant = await tenantDataRepository.GetAsync<Tenant>(mainTenant.Id, required: false);
            if (tenant != null)
            {
                return false;
            }

            if (!foxIDsEndpoint.IsNullOrWhiteSpace() && !foxIDsEndpoint.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            {
                mainTenant.CustomDomain = foxIDsEndpoint.UrlToDomain();
                mainTenant.CustomDomainVerified = true;
            }
            mainTenant.CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await tenantDataRepository.CreateAsync(mainTenant);
            return true;
        }
    }
}
