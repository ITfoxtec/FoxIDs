using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OidcDownPartyViewModel : IValidatableObject, IDownPartyName, IAllowUpPartyNames, IOAuthClaimTransformViewModel
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Down-party name (client ID / resource name)")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        [Display(Name = "Your notes")]
        public string Note { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax, Constants.Models.Party.NameLength, Constants.Models.Party.NameRegExPattern)]
        [Display(Name = "Allow up-party names (client IDs)")]
        public List<string> AllowUpPartyNames { get; set; } = new List<string>();

        /// <summary>
        /// OIDC down client.
        /// </summary>
        [ValidateComplexType]
        public OidcDownClientViewModel Client { get; set; }
  
        /// <summary>
        /// OAuth 2.0 down resource.
        /// </summary>
        [ValidateComplexType]        
        public OAuthDownResource Resource { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransformViewModel> ClaimTransforms { get; set; } = new List<OAuthClaimTransformViewModel>();

        /// <summary>
        /// URL party binding pattern.
        /// </summary>
        [Display(Name = "URL party binding pattern")]
        public PartyBindingPatterns PartyBindingPattern { get; set; } = PartyBindingPatterns.Brackets;

        /// <summary>
        /// Allow CORS origins.
        /// </summary>
        [ValidateComplexType]
        [Length(Constants.Models.OAuthDownParty.AllowCorsOriginsMin, Constants.Models.OAuthDownParty.AllowCorsOriginsMax, Constants.Models.OAuthDownParty.AllowCorsOriginLength)]
        [Display(Name = "Allow CORS origins")]
        public List<string> AllowCorsOrigins { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Client != null && AllowUpPartyNames?.Count <= 0)
            {
                results.Add(new ValidationResult($"At least one in the field {nameof(AllowUpPartyNames)} is required.", new[] { nameof(AllowUpPartyNames) }));
            }
            if (Client == null && Resource == null)
            {
                results.Add(new ValidationResult($"Either the field {nameof(Client)} or the field {nameof(Resource)} is required.", new[] { nameof(Client), nameof(Resource) }));
            }
            return results;
        }
    }
}
