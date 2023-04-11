using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class CreateUser
    {
        [Display(Name = "User must confirm account")]
        public bool ConfirmAccount { get; set; }

        [Display(Name = "Require multi-factor (2FA/MFA)")]
        public bool RequireMultiFactor { get; set; }

        [Length(Constants.Models.DynamicElements.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        [Display(Name = "Dynamic elements shown in order")]
        public List<DynamicElement> Elements { get; set; }

        /// <summary>
        /// Create user claim transforms, run after user creation.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [Display(Name = "Claim transforms executed in order")]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }
    }
}
