using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Seed
{
    public class MainTenantDocumentsSeedLogic : LogicBase 
    {
        private readonly IServiceProvider serviceProvider;
        private readonly Settings settings;
        private readonly ITenantDataRepository tenantDataRepository;

        public MainTenantDocumentsSeedLogic(IServiceProvider serviceProvider, Settings settings, ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.serviceProvider = serviceProvider;
            this.settings = settings;
            this.tenantDataRepository = tenantDataRepository;
        }

        public async Task<bool> SeedAsync()
        {
            try
            {
                if (!await CreateAndValidateMainTenantDocumentAsync(settings.FoxIDsEndpoint))
                {
                    return false;
                }

                var masterTenantLogic = serviceProvider.GetService<MasterTenantLogic>();
                await masterTenantLogic.CreateMasterTrackDocumentAsync(Constants.Routes.MainTenantName);
                var mLoginUpParty = await masterTenantLogic.CreateMasterLoginDocumentAsync(Constants.Routes.MainTenantName);
                await masterTenantLogic.CreateFirstAdminUserDocumentAsync(Constants.Routes.MainTenantName, Constants.DefaultAdminAccount.Email, Constants.DefaultAdminAccount.Password, true, false, false, isMasterTenant: true);
                await masterTenantLogic.CreateMasterFoxIDsControlApiResourceDocumentAsync(Constants.Routes.MainTenantName, isMasterTenant: true);
                await masterTenantLogic.CreateMasterControlClientDocumentAsync(Constants.Routes.MainTenantName, settings.FoxIDsControlEndpoint, mLoginUpParty, includeMasterTenantScope: true);

                await masterTenantLogic.CreateDefaultTracksDocmentsAsync(Constants.Routes.MainTenantName);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error seeding master tenant document.", ex);
            }
        }

        private async Task<bool> CreateAndValidateMainTenantDocumentAsync(string foxIDsEndpoint)
        {
            var mainTenant = new Tenant { ForUsage = false };
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
