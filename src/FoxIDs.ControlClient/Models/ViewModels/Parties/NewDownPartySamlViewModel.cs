using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class NewDownPartySamlViewModel
    {
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Technical name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Party.IssuerLength)]
        [Display(Name = "Application issuer")]
        public string Issuer { get; set; }

        [ListLength(Constants.Models.SamlParty.Down.AcsUrlsMin, Constants.Models.SamlParty.Down.AcsUrlsMax, Constants.Models.SamlParty.Down.AcsUrlsLength)]
        [Display(Name = "Assertion consumer service (ACS) URL")]
        public List<string> AcsUrls { get; set; }

        [Display(Name = "Absolute URLs")]
        public bool DisableAbsoluteUrls { get; set; } = true;

        [Display(Name = "SAML 2.0 metadata")]
        public string Metadata { get; set; }

        [Display(Name = "IdP Issuer")]
        public string MetadataIssuer { get; set; }

        [Display(Name = "Single Sign-On URL")]
        public string MetadataAuthn { get; set; }

        [Display(Name = "Single Logout URL")]
        public string MetadataLogout { get; set; }
    }
}
