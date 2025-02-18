using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class TrackLinkDownPartyViewModel : IDownPartyName, IValidatableObject, IAllowUpPartyNames
    {
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Client ID / Resource name)")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        [Display(Name = "Your notes")]
        public string Note { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameDbRegExPattern)]
        [Display(Name = "Link environment")]
        public string ToUpTrackName { get; set; }

        [Display(Name = "Link environment")]
        public string ToUpTrackDisplayName { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [Display(Name = "Link authentication method")]
        public string ToUpPartyName { get; set; }

        [Display(Name = "Link authentication method")]
        public string ToUpPartyDisplayName { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax)]
        [Display(Name = "Allow applications")]
        public List<UpPartyLink> AllowUpParties { get; set; } = new List<UpPartyLink>();

        [ValidateComplexType]
        [ListLength(Constants.Models.OAuthDownParty.Client.ClaimsMin, Constants.Models.OAuthDownParty.Client.ClaimsMax)]
        [Display(Name = "Issue claims (use * to issue all claims)")]
        public List<OAuthDownClaim> Claims { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<ClaimTransformViewModel> ClaimTransforms { get; set; } = new List<ClaimTransformViewModel>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (AllowUpParties?.Count <= 0)
            {
                results.Add(new ValidationResult($"At least one allowed authentication method is required.", [nameof(AllowUpParties)]));
            }
            return results;
        }
    }
}
