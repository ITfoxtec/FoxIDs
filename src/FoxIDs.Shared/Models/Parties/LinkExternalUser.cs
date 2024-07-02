using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class LinkExternalUser : IOAuthClaimTransforms, IValidatableObject
    {
        /// <summary>
        /// Automatic creation / provisioning of users
        /// </summary>
        [JsonProperty(PropertyName = "auto_create_user")]
        public bool AutoCreateUser { get; set; }

        [JsonProperty(PropertyName = "require_user")]
        public bool RequireUser { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [JsonProperty(PropertyName = "link_claim_type")]
        public string LinkClaimType { get; set; }

        [JsonProperty(PropertyName = "overwrite_claims")]
        public bool OverwriteClaims { get; set; }

        /// <summary>
        /// UI elements used for automatic creation.
        /// </summary>
        [ListLength(Constants.Models.DynamicElements.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        [JsonProperty(PropertyName = "elements")]
        public List<DynamicElement> Elements { get; set; }

        /// <summary>
        /// aAutomatic creation claim transforms, run after user creation before the user is saved.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [JsonProperty(PropertyName = "claim_transforms")]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (AutoCreateUser && RequireUser)
            {
                results.Add(new ValidationResult($"Both the {nameof(AutoCreateUser)} and the {nameof(RequireUser)} can not be enabled at the same time.", new[] { nameof(AutoCreateUser), nameof(RequireUser) }));
            }
            return results;
        }
    }
}
