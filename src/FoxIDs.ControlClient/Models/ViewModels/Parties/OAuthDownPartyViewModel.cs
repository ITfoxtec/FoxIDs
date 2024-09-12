using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OAuthDownPartyViewModel : IValidatableObject, IDownPartyName, IAllowUpPartyNames, IOAuthClaimTransformViewModel
    {
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Client ID / Resource name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        [Display(Name = "Your notes")]
        public string Note { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax)]
        [Display(Name = "Allow applications (client IDs)")]
        public List<UpPartyLink> AllowUpParties { get; set; } = new List<UpPartyLink>();

        /// <summary>
        /// OAuth 2.0 down client.
        /// </summary>
        [ValidateComplexType]
        public OAuthDownClientViewModel Client { get; set; }

        /// <summary>
        /// OAuth 2.0 down resource.
        /// </summary>
        [ValidateComplexType]
        public OAuthDownResource Resource { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransformViewModel> ClaimTransforms { get; set; } = new List<OAuthClaimTransformViewModel>();

        /// <summary>
        /// URL binding pattern.
        /// </summary>
        [Display(Name = "URL binding pattern")]
        public PartyBindingPatterns PartyBindingPattern { get; set; } = PartyBindingPatterns.Brackets;

        /// <summary>
        /// Allow CORS origins.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.OAuthDownParty.AllowCorsOriginsMin, Constants.Models.OAuthDownParty.AllowCorsOriginsMax, Constants.Models.OAuthDownParty.AllowCorsOriginLength)]
        [Display(Name = "Allow CORS origins")]
        public List<string> AllowCorsOrigins { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Client == null && Resource == null)
            {
                results.Add(new ValidationResult($"Either the field {nameof(Client)} or the field {nameof(Resource)} is required.", new[] { nameof(Client), nameof(Resource) }));
            }
            return results;
        }
    }
}
