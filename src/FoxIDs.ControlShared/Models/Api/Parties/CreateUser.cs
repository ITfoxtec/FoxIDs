using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class CreateUser
    {
        [Display(Name = "User must confirm account")]
        public bool ConfirmAccount { get; set; }

        [Display(Name = "User must change password")]
        public bool ChangePassword { get; set; }

        [Display(Name = "Require multi-factor (2FA/MFA)")]
        public bool RequireTwoFactor { get; set; }

        [Length(Constants.Models.CreateUser.ElementsMin, Constants.Models.CreateUser.ElementsMax)]
        [Display(Name = "")]
        public List<CreateUserElement> Elements { get; set; }

        /// <summary>
        /// Create user claim transforms, run after user creation.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }
    }
}
