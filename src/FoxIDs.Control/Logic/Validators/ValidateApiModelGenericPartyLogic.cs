using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ITfoxtec.Identity;
using System;
using System.Text.RegularExpressions;
using NetTools;
using System.Net;

namespace FoxIDs.Logic
{
    public class ValidateApiModelGenericPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ValidateApiModelDynamicElementLogic validateApiModelDynamicElementLogic;

        public ValidateApiModelGenericPartyLogic(TelemetryScopedLogger logger, ValidateApiModelDynamicElementLogic validateApiModelDynamicElementLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.validateApiModelDynamicElementLogic = validateApiModelDynamicElementLogic;
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

        public bool ValidateApiModelHrdIPAddressesAndRanges(ModelStateDictionary modelState, List<string> ipAddressesAndRanges, string errorFieldName)
        {
            var isValid = true;
            try
            {
                if (ipAddressesAndRanges?.Count() > 0)
                {
                    foreach (var ipar in ipAddressesAndRanges)
                    {
                        if (ipar.Contains('-') || ipar.Contains('/'))
                        {
                            try
                            {
                                IPAddressRange.Parse(ipar);
                            }
                            catch (Exception ex)
                            {
                                throw new ValidationException($"Invalid IP range '{ipar}'", ex);
                            }
                        }
                        else
                        {
                            try
                            {
                                IPAddress.Parse(ipar);
                            }
                            catch (Exception ex)
                            {
                                throw new ValidationException($"Invalid IP address '{ipar}'", ex);
                            }
                        }
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

        public bool ValidateApiModelHrdRegularExpressions(ModelStateDictionary modelState, List<string> regularExpressions, string errorFieldName)
        {
            var isValid = true;
            try
            {
                if (regularExpressions?.Count() > 0)
                {
                    foreach(var regex in regularExpressions)
                    {
                        try
                        {
                            new Regex(regex);
                        }
                        catch (Exception ex)
                        {
                            throw new ValidationException($"Invalid regular expression '{regex}'", ex);
                        }
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

        public bool ValidateExtendedUi(ModelStateDictionary modelState, List<Api.ExtendedUi> extendedUis)
        {
            var isValid = true;
            if (extendedUis?.Count() > 0)
            {
                foreach (var extendedUi in extendedUis)
                {
                    if (!validateApiModelDynamicElementLogic.ValidateApiModelExtendedUiElements(modelState, extendedUi.Elements))
                    {
                        isValid = false;
                    }

                    if (!ValidateApiModelClaimTransforms(modelState, extendedUi.ClaimTransforms))
                    {
                        isValid = false;
                    }
                } 
            }

            return isValid;
        }
    }
}
