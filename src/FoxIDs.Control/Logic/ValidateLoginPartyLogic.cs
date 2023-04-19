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

namespace FoxIDs.Logic
{
    public class ValidateLoginPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;

        public ValidateLoginPartyLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
        }

        public bool ValidateApiModel(ModelStateDictionary modelState, Api.LoginUpParty party)
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
                    if (!ValidateApiModelCreateUserElements(modelState, party.CreateUser.Elements))
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

        public bool ValidateApiModelCreateUserElements(ModelStateDictionary modelState, List<Api.DynamicElement> createUserElements) 
        {
            var isValid = true;
            try
            {
                if (createUserElements?.Count() > 0)
                {
                    var duplicatedOrderNumber = createUserElements.GroupBy(ct => ct.Order as int?).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
                    if (duplicatedOrderNumber >= 0)
                    {
                        throw new ValidationException($"Duplicated create user dynamic element order number '{duplicatedOrderNumber}'");
                    }

                    if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.EmailAndPassword).Count() != 1)
                    {
                        throw new ValidationException("Exactly one create user dynamic element of type EmailAndPassword is required.");
                    }

                    var duplicatedElementType = createUserElements.GroupBy(ct => ct.Type).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
                    if (duplicatedElementType > 0)
                    {
                        throw new ValidationException($"Duplicated create user dynamic element type '{duplicatedElementType}'");
                    }
                }
            }
            catch (ValidationException vex)
            {
                isValid = false;
                logger.Warning(vex);
                modelState.TryAddModelError(nameof(Api.LoginUpParty.CreateUser.Elements).ToCamelCase(), vex.Message);
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
