﻿using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models
{
    public class OidcDownParty : OidcDownParty<OidcDownClient, OidcDownScope, OidcDownClaim> { }
    public class OidcDownParty<TClient, TScope, TClaim> : OAuthDownParty<TClient, TScope, TClaim> where TClient : OidcDownClient<TScope, TClaim> where TScope : OidcDownScope<TClaim> where TClaim : OidcDownClaim
    {
        public OidcDownParty()
        {
            Type = PartyTypes.Oidc;
        }

        #region TestApp
        [MaxLength(IdentityConstants.MessageLength.CodeVerifierMax)]
        [JsonProperty(PropertyName = "code_verifier")]
        public string CodeVerifier { get; set; }

        [MaxLength(IdentityConstants.MessageLength.NonceMax)]
        [JsonProperty(PropertyName = "nonce")]
        public string Nonce { get; set; }
        #endregion

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            var baseResults = base.Validate(validationContext);
            if(baseResults.Count() > 0)
            {
                results.AddRange(baseResults);
            }
            if (Client != null && Client.ResponseTypes?.Count() > 0 && !(AllowUpParties?.Where(up => !up.DisableUserAuthenticationTrust)?.Count() > 0))
            {
                results.Add(new ValidationResult($"At least one (with user authentication trust) in the field {nameof(AllowUpParties)} is required if the Client is defined with a response type.", [nameof(Client), nameof(AllowUpParties), nameof(Client.ResponseTypes)]));
            }

            return results;
        }
    }
}
