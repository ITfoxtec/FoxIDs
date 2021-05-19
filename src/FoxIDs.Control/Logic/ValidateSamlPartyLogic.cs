using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using ITfoxtec.Identity.Saml2.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Models;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Logic
{
    public class ValidateSamlPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;

        public ValidateSamlPartyLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
        }

        public bool ValidateApiModel(ModelStateDictionary modelState, Api.SamlUpParty samlUpParty)
        {
            return ValidateSignatureAlgorithmAndSigningKeys(modelState, samlUpParty) && 
                ValidateLogout(modelState, samlUpParty);
        }

        public bool ValidateApiModel(ModelStateDictionary modelState, Api.SamlDownParty samlDownParty)
        {
            return ValidateSignatureAlgorithmAndSigningKeys(modelState, samlDownParty) &&
                ValidateLogout(modelState, samlDownParty);
        }

        private bool ValidateSignatureAlgorithmAndSigningKeys(ModelStateDictionary modelState, Api.SamlUpParty samlUpParty)
        {
            return ValidateSignatureAlgorithm(modelState, nameof(samlUpParty.SignatureAlgorithm), samlUpParty.SignatureAlgorithm) &&
                ValidateSigningKeys(modelState, nameof(samlUpParty.Keys), samlUpParty.Keys);
        }

        private bool ValidateSignatureAlgorithmAndSigningKeys(ModelStateDictionary modelState, Api.SamlDownParty samlDownParty)
        {
            return ValidateSignatureAlgorithm(modelState, nameof(samlDownParty.SignatureAlgorithm), samlDownParty.SignatureAlgorithm) &&
                ValidateSigningKeys(modelState, nameof(samlDownParty.Keys), samlDownParty.Keys);
        }

        private bool ValidateSigningKeys(ModelStateDictionary modelState, string propertyName, List<JsonWebKey> keys)
        {
            var isValid = true;
            try
            {
                if (keys != null)
                {
                    var anyDuplicate = keys.GroupBy(x => x.X5t).Any(g => g.Count() > 1);
                    if (anyDuplicate)
                    {
                        throw new ValidationException("Signature validation keys (certificates) has duplicates.");
                    }
                }
            }
            catch (ValidationException ex)
            {
                isValid = false;
                logger.Warning(ex);
                modelState.TryAddModelError(propertyName.ToCamelCase(), ex.Message);
            }
            return isValid;
        }

        private bool ValidateSignatureAlgorithm(ModelStateDictionary modelState, string propertyName, string signatureAlgorithm)
        {
            var isValid = true;
            try
            {
                SignatureAlgorithm.ValidateAlgorithm(signatureAlgorithm);
            }
            catch (NotSupportedException nsex)
            {
                isValid = false;
                var errorMessage = $"Signature algorithm '{signatureAlgorithm}' not supported.";
                logger.Warning(nsex, errorMessage);
                modelState.TryAddModelError(propertyName.ToCamelCase(), $"{errorMessage}{nsex.Message}");
            }
            return isValid;
        }

        private bool ValidateLogout(ModelStateDictionary modelState, Api.SamlUpParty samlUpParty)
        {
            var isValid = true;
            try
            {
                if (!samlUpParty.SingleLogoutResponseUrl.IsNullOrWhiteSpace() && samlUpParty.LogoutUrl.IsNullOrWhiteSpace())
                {
                    throw new Exception("Logout URL is required if single logout response URL is configured.");
                }
            }
            catch (Exception ex)
            {
                isValid = false;
                logger.Warning(ex);
                modelState.TryAddModelError(nameof(samlUpParty.LogoutUrl).ToCamelCase(), ex.Message);
            }

            if (!samlUpParty.LogoutUrl.IsNullOrWhiteSpace())
            {
                try
                {
                    if (samlUpParty.LogoutRequestBinding == null)
                    {
                        throw new Exception("Logout request binding is required.");
                    }
                }
                catch (Exception ex)
                {
                    isValid = false;
                    logger.Warning(ex);
                    modelState.TryAddModelError(nameof(samlUpParty.LogoutRequestBinding).ToCamelCase(), ex.Message);
                }

                try
                {
                    if (samlUpParty.LogoutResponseBinding == null)
                    {
                        throw new Exception("Logout response binding is required.");
                    }
                }
                catch (Exception ex)
                {
                    isValid = false;
                    logger.Warning(ex);
                    modelState.TryAddModelError(nameof(samlUpParty.LogoutResponseBinding).ToCamelCase(), ex.Message);
                }
            }
            else
            {
                samlUpParty.LogoutRequestBinding = null;
                samlUpParty.LogoutResponseBinding = null;
            }

            return isValid;
        }

        private bool ValidateLogout(ModelStateDictionary modelState, Api.SamlDownParty samlDownParty)
        {
            var isValid = true;

            if (!samlDownParty.LoggedOutUrl.IsNullOrWhiteSpace())
            {
                try
                {
                    if (samlDownParty.LogoutRequestBinding == null)
                    {
                        throw new Exception("Logout request binding is required.");
                    }
                }
                catch (Exception ex)
                {
                    isValid = false;
                    logger.Warning(ex);
                    modelState.TryAddModelError(nameof(samlDownParty.LogoutRequestBinding).ToCamelCase(), ex.Message);
                }

                try
                {
                    if (samlDownParty.LogoutResponseBinding == null)
                    {
                        throw new Exception("Logout response binding is required.");
                    }
                }
                catch (Exception ex)
                {
                    isValid = false;
                    logger.Warning(ex);
                    modelState.TryAddModelError(nameof(samlDownParty.LogoutResponseBinding).ToCamelCase(), ex.Message);
                }
            }
            else
            {
                samlDownParty.SingleLogoutUrl = null;
                samlDownParty.LogoutRequestBinding = null;
                samlDownParty.LogoutResponseBinding = null;
            }

            return isValid;
        }
    }
}
