using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Threading.Tasks;
using FoxIDs.Models;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Logic
{
    public class ValidateApiModelExternalUserLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantService;

        public ValidateApiModelExternalUserLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantService, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantService = tenantService;
        }

        public async Task<bool> ValidateApiModelAsync(ModelStateDictionary modelState, Api.ExternalUserId externalUserId)
        {
            var isValid = true;

            try
            {
                var upParty = await tenantService.GetAsync<UpParty>(await UpParty.IdFormatAsync(RouteBinding, externalUserId.UpPartyName), required: false);
                if (upParty == null)
                {
                    throw new ValidationException($"Up-party '{externalUserId.UpPartyName}' not found.");
                }
                else if (upParty.Type == PartyTypes.Login || upParty.Type == PartyTypes.OAuth2)
                {
                    throw new ValidationException($"External users can not be connected to up-party type '{upParty.Type}'.");
                }
            }
            catch (ValidationException vex)
            {
                isValid = false;
                logger.Warning(vex);
                modelState.TryAddModelError($"{nameof(Api.ExternalUserId.UpPartyName)}".ToCamelCase(), vex.Message);
            }

            return isValid;
        }
    }
}
