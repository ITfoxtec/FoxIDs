using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class LinkExternalUserViewModel : IValidatableObject, ILinkExternalUser
    {
        /// <summary>
        /// Automatic creation / provisioning of external users
        /// </summary>
        [Display(Name = "Automatically create/provision users")]
        public bool AutoCreateUser { get; set; }

        [Display(Name = "Require a user")]
        public bool RequireUser { get; set; }

        [Display(Name = "External user lifetime in seconds (0 to disable, sliding expiration)")]
        public int ExternalUserLifetime { get; set; } = 0;

        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [Display(Name = "Link claim type")]
        public string LinkClaimType { get; set; }

        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [Display(Name = "Redemption claim (inactive if empty)")]
        public string RedemptionClaimType { get; set; }

        [Display(Name = "Overwrite received claims")]
        public bool OverwriteClaims { get; set; }

        [ListLength(Constants.Models.DynamicElements.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        public List<DynamicElementViewModel> Elements { get; set; } = new List<DynamicElementViewModel>();

        [ListLength(Constants.Models.OAuthUpParty.Client.ClaimsMin, Constants.Models.OAuthUpParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeRegExPattern)]
        [Display(Name = "Include claims from authentication method")]
        public List<string> UpPartyClaims { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<ClaimTransformViewModel> ClaimTransforms { get; set; } = new List<ClaimTransformViewModel>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if ((AutoCreateUser || RequireUser || !RedemptionClaimType.IsNullOrWhiteSpace()) && LinkClaimType.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The link claim type is required.", [nameof(LinkClaimType)]));
            }
            if (AutoCreateUser && RequireUser)
            {
                results.Add(new ValidationResult($"Both the Automatically create/provision and the Require user can not be enabled at the same time.", [nameof(AutoCreateUser), nameof(RequireUser)]));
            }
            return results;
        }
    }
}
