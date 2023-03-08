using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class OidcUpClient : OAuthUpClient, IValidatableObject
    {
        [JsonProperty(PropertyName = "use_id_token_claims")]
        public bool UseIdTokenClaims { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>(base.Validate(validationContext));
            if (UseUserInfoClaims && UseIdTokenClaims)
            {
                results.Add(new ValidationResult($"The field {nameof(UseUserInfoClaims)} and the field {nameof(UseIdTokenClaims)} can not be enabled (true) at the same time.", new[] { nameof(UseUserInfoClaims), nameof(UseIdTokenClaims) }));
            }
            return results;
        }
    }
}
