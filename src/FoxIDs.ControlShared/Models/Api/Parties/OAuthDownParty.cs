using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.Api
{
    public class OAuthDownParty : IValidatableObject, IDownParty, INameValue, INewNameValue, IClaimTransformRef<OAuthClaimTransform>
    {
        /// <summary>
        /// Application name.
        /// </summary>
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

        /// <summary>
        /// OAuth 2.0 down client.
        /// </summary>
        [ValidateComplexType]
        public OAuthDownClient Client { get; set; }

        /// <summary>
        /// OAuth 2.0 down resource.
        /// </summary>
        [ValidateComplexType]
        public OAuthDownResource Resource { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        /// <summary>
        /// URL binding pattern.
        /// </summary>
        public PartyBindingPatterns PartyBindingPattern { get; set; } = PartyBindingPatterns.Brackets;

        /// <summary>
        /// Allow CORS origins.
        /// </summary>
        [ListLength(Constants.Models.OAuthDownParty.AllowCorsOriginsMin, Constants.Models.OAuthDownParty.AllowCorsOriginsMax, Constants.Models.OAuthDownParty.AllowCorsOriginLength)]
        public List<string> AllowCorsOrigins { get; set; }

        [Display(Name = "Use matching issuer and authority with application specific issuer")]
        public bool UsePartyIssuer { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Name.IsNullOrWhiteSpace() && DisplayName.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"Require either a Name or Display Name.", [nameof(Name), nameof(DisplayName)]));
            }
            if (Client == null && Resource == null)
            {
                results.Add(new ValidationResult($"Either the field {nameof(Client)} or the field {nameof(Resource)} is required.", [nameof(Client), nameof(Resource)]));
            }

            if (AllowUpPartyNames?.Count() > 0 && AllowUpParties?.Count() > 0)
            {
                results.Add(new ValidationResult($"The field {nameof(AllowUpParties)} and the field {nameof(AllowUpPartyNames)} can not be used at the same time. Pleas only use the field {nameof(AllowUpParties)}.", [nameof(AllowUpParties), nameof(AllowUpPartyNames)]));
            }
            return results;
        }
    }
}
