using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using FoxIDs.Models;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class ValidateApiModelLoginPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly PlanCacheLogic planCacheLogic;
        private readonly ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic;
        private readonly ValidateApiModelDynamicElementLogic validateApiModelDynamicElementLogic;

        public ValidateApiModelLoginPartyLogic(TelemetryScopedLogger logger, PlanCacheLogic planCacheLogic, ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic, ValidateApiModelDynamicElementLogic validateApiModelDynamicElementLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.planCacheLogic = planCacheLogic;
            this.validateApiModelGenericPartyLogic = validateApiModelGenericPartyLogic;
            this.validateApiModelDynamicElementLogic = validateApiModelDynamicElementLogic;
        }

        public async Task<bool> ValidateApiModelAsync(ModelStateDictionary modelState, Api.LoginUpParty party)
        {
            var isValid = true;

            if (!party.Css.IsNullOrWhiteSpace())
            {
                //TODO add validation
            }

            if (!party.IconUrl.IsNullOrWhiteSpace())
            {
                try
                {                   
                    var iconExtension = Path.GetExtension(party.IconUrl.Split('?')[0]);
                    _ = iconExtension switch
                    {
                        ".ico" => "image/x-icon",
                        ".png" => "image/png",
                        ".gif" => "image/gif",
                        ".jpeg" => "image/jpeg",
                        ".webp" => "image/webp",
                        _ => throw new ValidationException($"Icon image format '{iconExtension}' not supported.")
                    };
                }
                catch (ValidationException vex)
                {
                    isValid = false;
                    logger.Warning(vex);
                    modelState.TryAddModelError(nameof(Api.LoginUpParty.IconUrl).ToCamelCase(), vex.Message);
                }
            }

            if (party.TwoFactorAppName.IsNullOrWhiteSpace())
            {
                party.TwoFactorAppName = RouteBinding.TenantName;
            }

            if (party.CreateUser != null)
            {
                if (!party.EnableCreateUser || party.CreateUser.Elements?.Any() != true)
                {
                    party.CreateUser = null;
                }
                else
                {
                    if (!validateApiModelDynamicElementLogic.ValidateApiModelCreateUserElements(modelState, party.CreateUser.Elements, party.DisablePasswordAuth == true, party.EnablePasswordlessEmail == true, party.EnablePasswordlessSms == true))
                    {
                        isValid = false;
                    }

                    if (!validateApiModelGenericPartyLogic.ValidateApiModelClaimTransforms(modelState, party.CreateUser.ClaimTransforms, errorFieldName: nameof(Api.LoginUpParty.CreateUser.ClaimTransforms)))
                    {
                        isValid = false;
                    }
                }
            }

            return await Task.FromResult(isValid);
        }
    }
}
