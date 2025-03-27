using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.Api
{
    public class TrackLinkDownParty : IValidatableObject, IDownParty, INameValue, INewNameValue, IClaimTransform<OAuthClaimTransform>
    {        
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string NewName { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        public string Note { get; set; }

        [Obsolete($"Please use {nameof(AllowUpParties)} instead.")]
        [ListLength(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax, Constants.Models.Party.NameLength, Constants.Models.Party.NameRegExPattern)]
        public List<string> AllowUpPartyNames { get; set; }

        [ListLength(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax)]
        public List<UpPartyLink> AllowUpParties { get; set; }

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
                results.Add(new ValidationResult($"Require either a Name or Display Name.", [nameof(Name), nameof(DisplayName)]));
            }

            if (AllowUpPartyNames?.Count() > 0 && AllowUpParties?.Count() > 0)
            {
                results.Add(new ValidationResult($"The field {nameof(AllowUpParties)} and the field {nameof(AllowUpPartyNames)} can not be used at the same time. Pleas only use the field {nameof(AllowUpParties)}.", [nameof(AllowUpParties), nameof(AllowUpPartyNames)]));
            }
            return results;
        }
    }
}
