using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class TrackLinkDownPartyViewModel : IDownPartyName, IValidatableObject, IAllowUpPartyNames, IOAuthClaimTransformViewModel
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Down-party name (client ID / resource name)")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        [Display(Name = "Your notes")]
        public string Note { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameDbRegExPattern)]
        [Display(Name = "To track name")]
        public string ToUpTrackName { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [Display(Name = "To up-party name")]
        public string ToUpPartyName { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax, Constants.Models.Party.NameLength, Constants.Models.Party.NameRegExPattern)]
        [Display(Name = "Allow up-party names")]
        public List<string> AllowUpPartyNames { get; set; } = new List<string>();

        [ValidateComplexType]
        [Length(Constants.Models.OAuthDownParty.Client.ClaimsMin, Constants.Models.OAuthDownParty.Client.ClaimsMax)]
        [Display(Name = "Issue claims (use * to issue all claims)")]
        public List<OAuthDownClaim> Claims { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransformViewModel> ClaimTransforms { get; set; } = new List<OAuthClaimTransformViewModel>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (AllowUpPartyNames?.Count <= 0)
            {
                results.Add(new ValidationResult($"At least one in the field {nameof(AllowUpPartyNames)} is required.", new[] { nameof(AllowUpPartyNames) }));
            }
            return results;
        }
    }
}
