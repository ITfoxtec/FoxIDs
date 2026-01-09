using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class LinkExternalUser : IValidatableObject
    {
        /// <summary>
        /// Automatic creation / provisioning of external users
        /// </summary>
        [Display(Name = "Automatically create/provision users")]
        public bool AutoCreateUser { get; set; }

        [Display(Name = "Require a user")]
        public bool RequireUser { get; set; }

        /// <summary>
        /// External user lifetime in seconds. 0 disables expiration.
        /// </summary>
        [Display(Name = "External user lifetime in seconds (0 to disable)")]
        public int ExternalUserLifetime { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [Display(Name = "Link claim")]
        public string LinkClaimType { get; set; }

        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [Display(Name = "Redemption claim")]
        public string RedemptionClaimType { get; set; }

        [Display(Name = "Overwrite received claims")]
        public bool OverwriteClaims { get; set; }

        /// <summary>
        /// UI elements used for automatic creation.
        /// </summary>
        [ListLength(Constants.Models.DynamicElements.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        [Display(Name = "Dynamic elements shown in order")]
        public List<DynamicElement> Elements { get; set; }

        [ListLength(Constants.Models.OAuthUpParty.Client.ClaimsMin, Constants.Models.OAuthUpParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeRegExPattern)]
        [Display(Name = "Include claims from authentication method")]
        public List<string> UpPartyClaims { get; set; }

        /// <summary>
        /// aAutomatic creation claim transforms, run after user creation before the user is saved.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [Display(Name = "Claim transforms executed in order")]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (AutoCreateUser && RequireUser)
            {
                results.Add(new ValidationResult($"Both the {nameof(AutoCreateUser)} and the {nameof(RequireUser)} can not be enabled at the same time.", [nameof(AutoCreateUser), nameof(RequireUser)]));
            }
            return results;
        }
    }
}
