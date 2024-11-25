using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ITfoxtec.Identity;

namespace FoxIDs.Logic
{
    public class ValidateApiModelGenericPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;

        public ValidateApiModelGenericPartyLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
        }

        public bool ValidateApiModelClaimTransforms<T>(ModelStateDictionary modelState, List<T> claimTransforms, string errorFieldName = nameof(Api.OAuthDownParty.ClaimTransforms)) where T : Api.ClaimTransform
        {
            var isValid = true;
            try
            {
                if (claimTransforms?.Count() > 0)
                {
                    var duplicatedName = claimTransforms.Where(c => !c.Name.IsNullOrWhiteSpace()).GroupBy(ct => ct.Name).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
                    if (!duplicatedName.IsNullOrEmpty())
                    {
                        throw new ValidationException($"Duplicated claim transform name '{duplicatedName}'");
                    }

                    var duplicatedOrderNumber = claimTransforms.GroupBy(ct => ct.Order as int?).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
                    if (duplicatedOrderNumber >= 0)
                    {
                        throw new ValidationException($"Duplicated claim transform order number '{duplicatedOrderNumber}'");
                    }
                }
            }
            catch (ValidationException vex)
            {
                isValid = false;
                logger.Warning(vex);
                modelState.TryAddModelError(errorFieldName.ToCamelCase(), vex.Message);
            }
            return isValid;
        }
    }
}
