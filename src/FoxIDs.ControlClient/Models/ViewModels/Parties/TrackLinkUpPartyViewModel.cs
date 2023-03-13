using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class TrackLinkUpPartyViewModel : IOAuthClaimTransformViewModel, IUpPartyHrd
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Up-party name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameDbRegExPattern)]
        [Display(Name = "To track name")]
        public string ToDownTrackName { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [Display(Name = "To down-party name")]
        public string ToDownPartyName { get; set; }

        [Length(Constants.Models.TrackLinkDownParty.SelectedUpPartiesMin, Constants.Models.TrackLinkDownParty.SelectedUpPartiesMax, Constants.Models.Party.NameLength, Constants.Models.TrackLinkDownParty.SelectedUpPartiesNameRegExPattern)]
        [Display(Name = "Selected up-parties (use * to select all up-parties)")]
        public List<string> SelectedUpParties { get; set; } = new List<string>(new[] { "*" });

        [Length(Constants.Models.OAuthUpParty.Client.ClaimsMin, Constants.Models.OAuthUpParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [Display(Name = "Forward claims (use * to carried all claims forward)")]
        public List<string> Claims { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransformViewModel> ClaimTransforms { get; set; } = new List<OAuthClaimTransformViewModel>();

        /// <summary>
        /// Home realm discovery (HRD) domains.
        /// </summary>
        [Length(Constants.Models.UpParty.HrdDomainMin, Constants.Models.UpParty.HrdDomainMax, Constants.Models.UpParty.HrdDomainLength, Constants.Models.UpParty.HrdDomainRegExPattern)]
        [Display(Name = "HRD domains")]
        public List<string> HrdDomains { get; set; }

        [Display(Name = "Show HRD button with domain")]
        public bool HrdShowButtonWithDomain { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) display name.
        /// </summary>
        [MaxLength(Constants.Models.UpParty.HrdDisplayNameLength)]
        [RegularExpression(Constants.Models.UpParty.HrdDisplayNameRegExPattern)]
        [Display(Name = "HRD display name")]
        public string HrdDisplayName { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) logo URL.
        /// </summary>
        [MaxLength(Constants.Models.UpParty.HrdLogoUrlLength)]
        [RegularExpression(Constants.Models.UpParty.HrdLogoUrlRegExPattern)]
        [Display(Name = "HRD logo URL")]
        public string HrdLogoUrl { get; set; }
    }
}
