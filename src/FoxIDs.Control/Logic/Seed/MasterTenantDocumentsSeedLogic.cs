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

        public async Task SeedAsync()
        {
            try
            {
                await CreateAndValidateMasterTenantDocumentAsync();

                await masterTenantLogic.CreateMasterTrackDocumentAsync(Constants.Routes.MasterTenantName, TrackKeyTypes.KeyVaultRenewSelfSigned);
                var mLoginUpParty = await masterTenantLogic.CreateMasterLoginDocumentAsync(Constants.Routes.MasterTenantName);
                await masterTenantLogic.CreateFirstAdminUserDocumentAsync(Constants.Routes.MasterTenantName, Constants.DefaultAdminAccount.Email, Constants.DefaultAdminAccount.Password, true, false, false, isMasterTenant: true);
                await masterTenantLogic.CreateMasterFoxIDsControlApiResourceDocumentAsync(Constants.Routes.MasterTenantName, isMasterTenant: true);
                await masterTenantLogic.CreateMasterControlClientDocmentAsync(Constants.Routes.MasterTenantName, settings.FoxIDsControlEndpoint, mLoginUpParty, includeMasterTenantScope: true);
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
                _ = await tenantDataRepository.GetAsync<Tenant>(masterTenant.Id);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.Conflict)
                {
                    throw new Exception($"{masterTenant.Id} document exists.");
                }
            }

            await tenantDataRepository.CreateAsync(masterTenant);
        }
    }
}
