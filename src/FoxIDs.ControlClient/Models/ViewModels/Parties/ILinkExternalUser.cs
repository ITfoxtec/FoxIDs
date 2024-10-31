using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface ILinkExternalUser : IOAuthClaimTransformViewModel, IDynamicElementsViewModel
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

        [Display(Name = "Overwrite revived claims")]
        public bool OverwriteClaims { get; set; }
    }
}
