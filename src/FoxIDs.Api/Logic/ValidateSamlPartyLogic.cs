using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using ITfoxtec.Identity.Saml2.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace FoxIDs.Logic
{
    public class ValidateSamlPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;

        public ValidateSamlPartyLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
        }

        public bool ValidateSignatureAlgorithm(ModelStateDictionary modelState, Api.SamlUpParty samlUpParty) => ValidateSignatureAlgorithm(modelState, nameof(samlUpParty.SignatureAlgorithm), samlUpParty.SignatureAlgorithm);
        public bool ValidateSignatureAlgorithm(ModelStateDictionary modelState, Api.SamlDownParty samlDownParty) => ValidateSignatureAlgorithm(modelState, nameof(samlDownParty.SignatureAlgorithm), samlDownParty.SignatureAlgorithm);

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
