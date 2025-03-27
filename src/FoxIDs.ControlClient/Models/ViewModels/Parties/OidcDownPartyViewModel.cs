using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OidcDownPartyViewModel : IValidatableObject, IDownPartyName, IAllowUpPartyNames
    {
        public string InitName { get; set; }

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
        [ValidateComplexType]
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<ClaimTransformViewModel> ClaimTransforms { get; set; } = new List<ClaimTransformViewModel>();

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

        [Display(Name = "Use matching issuer and authority with application specific issuer")]
        public bool UsePartyIssuer { get; set; }

        /// <summary>
        /// Is test.
        /// </summary>
        public bool IsTest { get; set; }

        /// <summary>
        /// Test URL
        /// </summary>
        [MaxLength(Constants.Models.DownParty.UrlLengthMax)]
        public string TestUrl { get; set; }

        /// <summary>
        /// Test expiration time.
        /// </summary>
        public long TestExpireAt { get; set; }

        /// <summary>
        /// Test expiration in seconds.
        /// </summary>
        [Display(Name = "Expiration time in seconds (0 to disable)")]
        public int TestExpireInSeconds { get; set; } = 900;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Client != null && AllowUpParties?.Count <= 0)
            {
                results.Add(new ValidationResult($"At least one allowed authentication method is required.", [nameof(AllowUpParties)]));
            }
            if (Client == null && Resource == null)
            {
                results.Add(new ValidationResult($"Either the field {nameof(Client)} or the field {nameof(Resource)} is required.", [nameof(Client), nameof(Resource)]));
            }
            return results;
        }
    }
}
