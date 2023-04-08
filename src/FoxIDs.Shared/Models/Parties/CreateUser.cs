using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models
{
    public class CreateUser
    {
        [JsonProperty(PropertyName = "confirm_account")]
        public bool ConfirmAccount { get; set; }

        [JsonProperty(PropertyName = "change_password")]
        public bool ChangePassword { get; set; }

        [JsonProperty(PropertyName = "require_two_factor")]
        public bool RequireTwoFactor { get; set; }

        [Length(Constants.Models.CreateUser.ElementsMin, Constants.Models.CreateUser.ElementsMax)]
        [JsonProperty(PropertyName = "elements")]
        public List<CreateUserElement> Elements { get; set; }

        /// <summary>
        /// Create user claim transforms, run after user creation.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [JsonProperty(PropertyName = "claim_transforms")]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }
    }
}
