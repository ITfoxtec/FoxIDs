using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models
{
    public class CreateUser
    {
        [JsonProperty(PropertyName = "confirm_account")]
        public bool ConfirmAccount { get; set; }

        [JsonProperty(PropertyName = "require_multi_factor")]
        public bool RequireMultiFactor { get; set; }

        [Length(Constants.Models.DynamicElements.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        [JsonProperty(PropertyName = "elements")]
        public List<DynamicElement> Elements { get; set; }

        /// <summary>
        /// Create user claim transforms, run after user creation.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [JsonProperty(PropertyName = "claim_transforms")]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }
    }
}
