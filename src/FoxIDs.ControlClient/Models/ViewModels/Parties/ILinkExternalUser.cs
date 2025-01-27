using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface ILinkExternalUser : IDynamicElementsViewModel
    {
        [Display(Name = "Optional create/provision external users automatically")]
        public bool AutoCreateUser { get; set; }

        [Display(Name = "Optional require external user")]
        public bool RequireUser { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [Display(Name = "Link claim type")]
        public string LinkClaimType { get; set; }

        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [Display(Name = "Redemption claim type (inactive if empty)")]
        public string RedemptionClaimType { get; set; }

        [Display(Name = "Overwrite received claims")]
        public bool OverwriteClaims { get; set; }

        [ListLength(Constants.Models.OAuthUpParty.Client.ClaimsMin, Constants.Models.OAuthUpParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeRegExPattern)]
        [Display(Name = "Include claims from authentication method")]
        public List<string> UpPartyClaims { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<ClaimTransformViewModel> ClaimTransforms { get; set; }
    }
}
