using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using FoxIDs.Models;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class ValidateApiModelLoginPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly PlanCacheLogic planCacheLogic;
        private readonly ValidateApiModelDynamicElementLogic validateApiModelDynamicElementLogic;

        public ValidateApiModelLoginPartyLogic(TelemetryScopedLogger logger, PlanCacheLogic planCacheLogic, ValidateApiModelDynamicElementLogic validateApiModelDynamicElementLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.planCacheLogic = planCacheLogic;
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
                    if (!validateApiModelDynamicElementLogic.ValidateApiModelCreateUserElements(modelState, party.CreateUser.Elements))
                    {
                        isValid = false;
                    }

                    if (!ValidateApiModelCreateUserClaimTransforms(modelState, party.CreateUser.ClaimTransforms))
                    {
                        isValid = false;
                    }
                }
            }

            return isValid;
        }

        public bool ValidateApiModelCreateUserClaimTransforms(ModelStateDictionary modelState, List<Api.OAuthClaimTransform> claimTransforms) 
        {
            var isValid = true;
            try
            {
                if (claimTransforms?.Count() > 0)
                {
                    var duplicatedOrderNumber = claimTransforms.GroupBy(ct => ct.Order as int?).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
                    if (duplicatedOrderNumber >= 0)
                    {
                        throw new ValidationException($"Duplicated create user claim transform order number '{duplicatedOrderNumber}'");
                    }
                }
            }
            catch (ValidationException vex)
            {
                isValid = false;
                logger.Warning(vex);
                modelState.TryAddModelError(nameof(Api.LoginUpParty.CreateUser.ClaimTransforms).ToCamelCase(), vex.Message);
            }
            return isValid;
        }
    }
}
