using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Down-party that links to another track for authentication.
    /// </summary>
    public class TrackLinkDownParty : IValidatableObject, IDownParty, INameValue, INewNameValue, IClaimTransformRef<OAuthClaimTransform>
    {        
        /// <summary>
        /// Technical party name.
        /// </summary>
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        /// <summary>
        /// New name used when renaming the party.
        /// </summary>
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string NewName { get; set; }

        /// <summary>
        /// Display friendly party name.
        /// </summary>
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Optional note about the party.
        /// </summary>
        [MaxLength(Constants.Models.Party.NoteLength)]
        public string Note { get; set; }

        [Obsolete($"Please use {nameof(AllowUpParties)} instead.")]
        [ListLength(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax, Constants.Models.Party.NameLength, Constants.Models.Party.NameRegExPattern)]
        public List<string> AllowUpPartyNames { get; set; }

        /// <summary>
        /// Allowed upstream parties.
        /// </summary>
        [ListLength(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax)]
        public List<UpPartyLink> AllowUpParties { get; set; }

        /// <summary>
        /// Name of the upstream track being linked to.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameDbRegExPattern)]
        public string ToUpTrackName { get; set; }

        /// <summary>
        /// Name of the upstream party in the linked track.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string ToUpPartyName { get; set; }

        /// <summary>
        /// Claims to forward to the upstream track.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.OAuthDownParty.Client.ClaimsMin, Constants.Models.OAuthDownParty.Client.ClaimsMax)]
        public List<OAuthDownClaim> Claims { get; set; }

        /// <summary>
        /// Claim transforms executed before forwarding.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
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
