using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models
{
    public class TrackLinkUpParty : UpPartyWithExternalUser<TrackLinkUpPartyProfile>, IOAuthClaimTransforms, IValidatableObject
    {
        public TrackLinkUpParty()
        {
            Type = PartyTypes.TrackLink;
        }

        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameDbRegExPattern)]
        [JsonProperty(PropertyName = "to_down_track_name")]
        public string ToDownTrackName { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [JsonProperty(PropertyName = "to_down_party_name")]
        public string ToDownPartyName { get; set; }

        [ListLength(Constants.Models.TrackLinkDownParty.SelectedUpPartiesMin, Constants.Models.TrackLinkDownParty.SelectedUpPartiesMax, Constants.Models.Party.NameLength, Constants.Models.TrackLinkDownParty.SelectedUpPartiesNameRegExPattern)]
        [JsonProperty(PropertyName = "selected_up_parties")]
        public List<string> SelectedUpParties { get; set; }

        [ListLength(Constants.Models.OAuthUpParty.Client.ClaimsMin, Constants.Models.OAuthUpParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [JsonProperty(PropertyName = "claims")]
        public List<string> Claims { get; set; }

        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [JsonProperty(PropertyName = "claim_transforms")]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        [JsonProperty(PropertyName = "pipe_external_id")]
        public bool PipeExternalId { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            var baseResults = base.Validate(validationContext);
            if (baseResults.Count() > 0)
            {
                results.AddRange(baseResults);
            }

            if (ClaimTransforms?.Count() + ExternalUserLoadedClaimTransforms?.Count() > Constants.Models.Claim.TransformsMax)
            {
                results.Add(new ValidationResult($"The number of claims transforms in '{nameof(ClaimTransforms)}' and '{nameof(ExternalUserLoadedClaimTransforms)}' can be a  of {Constants.Models.Claim.TransformsMax} combined.", [nameof(ClaimTransforms), nameof(ExternalUserLoadedClaimTransforms)]));
            }
            return results;
        }
    }
}
