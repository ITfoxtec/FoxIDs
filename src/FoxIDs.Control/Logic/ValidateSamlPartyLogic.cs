using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using ITfoxtec.Identity.Saml2.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using FoxIDs.Models.Api;
using System.Linq;

namespace FoxIDs.Logic
{
    public class ValidateSamlPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;

        public ValidateSamlPartyLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
        }

        public bool ValidateSignatureAlgorithmAndSigningKeys(ModelStateDictionary modelState, Api.SamlUpParty samlUpParty)
        {
            return ValidateSignatureAlgorithm(modelState, nameof(samlUpParty.SignatureAlgorithm), samlUpParty.SignatureAlgorithm) &&
                ValidateSigningKeys(modelState, nameof(samlUpParty.Keys), samlUpParty.Keys);
        }

        public bool ValidateSignatureAlgorithm(ModelStateDictionary modelState, Api.SamlDownParty samlDownParty)
        {
            return ValidateSignatureAlgorithm(modelState, nameof(samlDownParty.SignatureAlgorithm), samlDownParty.SignatureAlgorithm);
        }

        private bool ValidateSigningKeys(ModelStateDictionary modelState, string propertyName, List<JsonWebKey> keys)
        {
            var isValid = true;
            try
            {
                var anyDuplicate = keys.GroupBy(x => x.X5t).Any(g => g.Count() > 1);
                if (anyDuplicate)
                {
                    throw new Exception("Signing keys has duplicates.");
                }
            }
            catch (Exception ex)
            {
                isValid = false;
                logger.Warning(ex);
                modelState.TryAddModelError(propertyName.ToCamelCase(), ex.Message);
                return false;
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

    }
}
