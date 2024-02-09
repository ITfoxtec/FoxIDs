using Api = FoxIDs.Models.Api;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ITfoxtec.Identity;
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

        public bool ValidateApiModel(ModelStateDictionary modelState, OidcUpParty party)
        {
            var isValid = true;
            try
            {
                if (party.Client != null)
                {
                    if (party.Client.ResponseType.Contains(IdentityConstants.ResponseTypes.Code) == true)
                    {
                        if (party.Client.ClientAuthenticationMethod != ClientAuthenticationMethods.PrivateKeyJwt && party.Client.ClientSecret.IsNullOrEmpty())
                        {
                            throw new ValidationException($"Require '{nameof(OidcUpParty.Client)}.{nameof(party.Client.ClientSecret)}' or '{nameof(OidcUpParty.Client)}.{nameof(party.Client.ClientAuthenticationMethod)}={ClientAuthenticationMethods.PrivateKeyJwt}' to execute '{IdentityConstants.ResponseTypes.Code}' response type.");
                        }
                    }
                }
            }
            catch (ValidationException vex)
            {
                isValid = false;
                logger.Warning(vex);
                modelState.TryAddModelError($"{nameof(Api.OidcUpParty.Client)}.{nameof(Api.OidcUpParty.Client.ClientSecret)}".ToCamelCase(), vex.Message);
            }
            return isValid;
        }
    }
}
