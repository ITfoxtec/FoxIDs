using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Logic
{
    public class ValidateModelOAuthOidcPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;

        public ValidateModelOAuthOidcPartyLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
        }

        public bool ValidateModel(ModelStateDictionary modelState, OidcUpParty party)
        {
            var isValid = true;
            try
            {
                if (party.Client?.ClientAuthenticationMethod == ClientAuthenticationMethods.PrivateKeyJwt && party.Client.ClientKeys == null)
                {

                    throw new ValidationException($"The Client Key need to be set before the {nameof(party.Client.ClientAuthenticationMethod)} can be set to '{ClientAuthenticationMethods.PrivateKeyJwt}'");

                }
            }
            catch (ValidationException vex)
            {
                isValid = false;
                logger.Warning(vex);
                modelState.TryAddModelError($"{nameof(party.Client.ClientAuthenticationMethod)}.{nameof(party.Client.ClientKeys)}".ToCamelCase(), vex.Message);
            }
            return isValid;
        }
    }
}
