using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OidcDownClientViewModel : OidcDownClient, IClientSecret, IClientResourceScope, IValidatableObject
    {
        public OidcDownClientViewModel()
        {
            DefaultResourceScopeScopes = new List<string>();
            ResourceScopes = new List<OAuthDownResourceScope>();
            Scopes = new List<OidcDownScope>();
            Claims = new List<OidcDownClaim>();
            ResponseTypes = new List<string>();
            ExistingSecrets = new List<OAuthClientSecretViewModel>();
            Secrets = new List<string>();
        }

        [ValidateComplexType]
        [Length(0, Constants.Models.OAuthDownParty.Client.ResourceScopesMax)]
        [Display(Name = "Resource and scopes")]
        public new List<OAuthDownResourceScope> ResourceScopes { get; set; }

        public bool DefaultResourceScope { get; set; } = true;

        public List<string> DefaultResourceScopeScopes { get; set; }

        public List<OAuthClientSecretViewModel> ExistingSecrets { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.OAuthDownParty.Client.SecretsMin, Constants.Models.OAuthDownParty.Client.SecretsMax, Constants.Models.SecretHash.SecretLength)]
        [Display(Name = "Secrets")]
        public List<string> Secrets { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (!RequirePkce && Secrets.Count() <= 0 && ExistingSecrets.Where(s => !s.Removed).Count() <= 0)
            {
                results.Add(new ValidationResult($"The field Secrets must be between 1 and 10 if PKCE is not require.", new[] { nameof(Secrets) }));
            }
            if (!DefaultResourceScope && ResourceScopes.Count <= 0)
            {
                results.Add(new ValidationResult($"The field Resource and scopes must be between 1 and 50 if default Resource Scope is not selected.", new[] { nameof(ResourceScopes) }));
            }
            return results;
        }
    }
}
