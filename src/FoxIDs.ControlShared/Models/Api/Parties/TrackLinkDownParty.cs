using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackLinkDownParty : IValidatableObject, IDownParty, INameValue, IClaimTransform<OAuthClaimTransform>
    {        
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        public string Note { get; set; }

        [ListLength(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax, Constants.Models.Party.NameLength, Constants.Models.Party.NameRegExPattern)]
        public List<string> AllowUpPartyNames { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameDbRegExPattern)]
        [JsonProperty(PropertyName = "to_up_track_name")]
        public string ToUpTrackName { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [JsonProperty(PropertyName = "to_up_party_name")]
        public string ToUpPartyName { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.OAuthDownParty.Client.ClaimsMin, Constants.Models.OAuthDownParty.Client.ClaimsMax)]
        public List<OAuthDownClaim> Claims { get; set; }

        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [JsonProperty(PropertyName = "claim_transforms")]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Name.IsNullOrWhiteSpace() && DisplayName.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"Require either a Name or Display Name.", new[] { nameof(Name), nameof(DisplayName) }));
            }
            return results;
        }
    }
}
