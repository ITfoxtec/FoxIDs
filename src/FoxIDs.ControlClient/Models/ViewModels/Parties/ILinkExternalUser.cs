using FoxIDs.Infrastructure.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface ILinkExternalUser : IOAuthClaimTransformViewModel, IDynamicElementsViewModel
    {
        [Display(Name = "Automatically create/provision users")]
        public bool AutoCreateUser { get; set; }

        [Display(Name = "Require a user")]
        public bool RequireUser { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [Display(Name = "Link claim")]
        public string LinkClaimType { get; set; }

        [Display(Name = "Overwrite revived claims")]
        public bool OverwriteClaims { get; set; }
    }
}
