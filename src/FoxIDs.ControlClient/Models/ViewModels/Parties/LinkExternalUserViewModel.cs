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

        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [Display(Name = "Link claim")]
        public string LinkClaimType { get; set; }

        [Display(Name = "Overwrite revived claims")]
        public bool OverwriteClaims { get; set; }

        [ListLength(Constants.Models.DynamicElements.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        public List<DynamicElementViewModel> Elements { get; set; } = new List<DynamicElementViewModel>();

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransformViewModel> ClaimTransforms { get; set; } = new List<OAuthClaimTransformViewModel>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if ((AutoCreateUser || RequireUser) && LinkClaimType.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The link claim type is required.", new[] { nameof(LinkClaimType) }));
            }
            if (AutoCreateUser && RequireUser)
            {
                results.Add(new ValidationResult($"Both the Automatically create/provision and the Require user can not be enabled at the same time.", new[] { nameof(AutoCreateUser), nameof(RequireUser) }));
            }
            return results;
        }
    }
}
