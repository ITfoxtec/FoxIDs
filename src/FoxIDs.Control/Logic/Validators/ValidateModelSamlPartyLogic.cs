using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Threading.Tasks;
using Api = FoxIDs.Models.Api;

namespace FoxIDs.Logic
{
    public class ValidateModelSamlPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;

        public ValidateModelSamlPartyLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
        }

        public async ValueTask<bool> ValidateModelAsync(ModelStateDictionary modelState, Api.SamlUpParty apiParty, SamlUpParty party)
        {
            return await ValidateAndLockModuleTypeAsync(modelState, apiParty, party);
        }

        private async ValueTask<bool> ValidateAndLockModuleTypeAsync(ModelStateDictionary modelState, Api.SamlUpParty apiParty, SamlUpParty modelParty)
        {
            var existingParty = await tenantDataRepository.GetAsync<SamlUpParty>(await UpParty.IdFormatAsync(RouteBinding, apiParty.Name), required: false);
            if (existingParty == null)
            {
                return true;
            }

            if (existingParty.ModuleType != modelParty.ModuleType)
            {
                modelState.TryAddModelError(nameof(apiParty.ModuleType).ToCamelCase(), "The authentication method module type can not be changed.");
                return false;
            }

            modelParty.ModuleType = existingParty.ModuleType;
            return true;
        }
    }
}
