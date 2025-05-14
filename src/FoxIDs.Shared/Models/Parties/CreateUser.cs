using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models
{
    public class CreateUser : IOAuthClaimTransforms
    {
        /// <summary>
        /// Passwordless with email require the user to have a email user identifier.
        /// </summary>
        [JsonProperty(PropertyName = "passwordless_email")]
        public bool PasswordlessEmail { get; set; }

        /// <summary>
        /// Passwordless with SMS require the user to have a phone user identifier.
        /// </summary>
        [JsonProperty(PropertyName = "passwordless_sms")]
        public bool PasswordlessSms { get; set; }

        [JsonProperty(PropertyName = "confirm_account")]
        public bool ConfirmAccount { get; set; }

        [JsonProperty(PropertyName = "require_multi_factor")]
        public bool RequireMultiFactor { get; set; }

        [ListLength(Constants.Models.DynamicElements.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        [JsonProperty(PropertyName = "elements")]
        public List<DynamicElement> Elements { get; set; }

        /// <summary>
        /// Create user claim transforms, run after user creation.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [JsonProperty(PropertyName = "claim_transforms")]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }  
    }
}
