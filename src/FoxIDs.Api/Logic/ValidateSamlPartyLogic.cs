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

        public bool ValidateSignatureAlgorithm(ModelStateDictionary modelState, Api.SamlUpParty samlUpParty)
        {
            var isValid = true;
            try
            {
                SignatureAlgorithm.ValidateAlgorithm(samlUpParty.SignatureAlgorithm);
            }
            catch (NotSupportedException nsex)
            {
                isValid = false;
                var errorMessage = $"Signature algorithm '{samlUpParty.SignatureAlgorithm}' not supported.";
                logger.Warning(nsex, errorMessage);
                modelState.TryAddModelError(nameof(samlUpParty.SignatureAlgorithm).ToCamelCase(), $"{errorMessage}{nsex.Message}");
            }
            return isValid;
        }

    }
}
