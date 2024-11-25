using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface ILinkExternalUser : IClaimTransformViewModel, IDynamicElementsViewModel
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

        [Display(Name = "Overwrite received claims")]
        public bool OverwriteClaims { get; set; }
    }
}
