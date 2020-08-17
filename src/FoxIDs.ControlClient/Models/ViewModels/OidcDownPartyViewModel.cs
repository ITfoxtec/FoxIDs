using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OidcDownPartyViewModel : IValidatableObject, IAllowUpPartyNames
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Down Party name")]
        public string Name { get; set; }

        [Length(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax, Constants.Models.Party.NameLength, Constants.Models.Party.NameRegExPattern)]
        [Display(Name = "Allow Up Party names")]
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
        public OAuthDownResourceViewModel Resource { get; set; } 

        /// <summary>
        /// Allow cors origins.
        /// </summary>
        [Length(Constants.Models.OAuthDownParty.AllowCorsOriginsMin, Constants.Models.OAuthDownParty.AllowCorsOriginsMax, Constants.Models.OAuthDownParty.AllowCorsOriginLength)]
        [Display(Name = "Allow cors origins")]
        public List<string> AllowCorsOrigins { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Client != null && AllowUpPartyNames?.Count <= 0)
            {
                results.Add(new ValidationResult($"At least one in the field {nameof(AllowUpPartyNames)} is required if the field {nameof(Resource)} is defined.", new[] { nameof(Client), nameof(AllowUpPartyNames) }));
            }
            if (Client == null && Resource == null)
            {
                results.Add(new ValidationResult($"Either the field {nameof(Client)} or the field {nameof(Resource)} is required.", new[] { nameof(Client), nameof(Resource) }));
            }
            return results;
        }
    }
}
