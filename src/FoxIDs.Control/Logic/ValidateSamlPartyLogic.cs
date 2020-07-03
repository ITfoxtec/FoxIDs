using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using ITfoxtec.Identity.Saml2.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using FoxIDs.Models.Api;
using System.Linq;
using ITfoxtec.Identity;

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
                var anyDuplicate = keys.GroupBy(x => x.X5t).Any(g => g.Count() > 1);
                if (anyDuplicate)
                {
                    throw new Exception("Signature validation keys (certificates) has duplicates.");
                }
            }
            catch (Exception ex)
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

        private bool ValidateLogout(ModelStateDictionary modelState, SamlUpParty samlUpParty)
        {
            var isValid = true;
            try
            {
                if(!samlUpParty.LogoutUrl.IsNullOrWhiteSpace())
                {
                    if(samlUpParty.LogoutBinding == null)
                    {
                        throw new Exception("Logout binding is required.");
                    }
                }
                else
                {
                    samlUpParty.LogoutBinding = null;
                }
            }
            catch (Exception ex)
            {
                isValid = false;
                logger.Warning(ex);
                modelState.TryAddModelError(nameof(samlUpParty.LogoutBinding).ToCamelCase(), ex.Message);
            }
            return isValid;
        }

        private bool ValidateLogout(ModelStateDictionary modelState, SamlDownParty samlDownParty)
        {
            var isValid = true;
            try
            {
                if (!samlDownParty.LoggedOutUrl.IsNullOrWhiteSpace())
                {
                    if (samlDownParty.LogoutBinding == null)
                    {
                        throw new Exception("Logout binding is required.");
                    }
                }
                else
                {
                    samlDownParty.SingleLogoutUrl = null;
                    samlDownParty.LogoutBinding = null;
                }
            }
            catch (Exception ex)
            {
                isValid = false;
                logger.Warning(ex);
                modelState.TryAddModelError(nameof(samlDownParty.LogoutBinding).ToCamelCase(), ex.Message);
            }
            return isValid;
        }
    }
}
