using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
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
            ScopesViewModel = new List<OidcDownScopeViewModel>();
            Claims = new List<OidcDownClaim>();
            ResponseTypes = new List<string>();
            ExistingSecrets = new List<OAuthClientSecretViewModel>();
            Secrets = new List<string>();
        }

        [ValidateComplexType]
        [Length(0, Constants.Models.OAuthDownParty.Client.ResourceScopesMax)]
        [Display(Name = "Resource and scopes")]
        public new List<OAuthDownResourceScope> ResourceScopes { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.OAuthDownParty.Client.ScopesMin, Constants.Models.OAuthDownParty.Client.ScopesMax)]
        [Display(Name = "Scopes")]
        public List<OidcDownScopeViewModel> ScopesViewModel { get; set; }

        public bool DefaultResourceScope { get; set; } = true;

        public List<string> DefaultResourceScopeScopes { get; set; }

        public List<OAuthClientSecretViewModel> ExistingSecrets { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.OAuthDownParty.Client.SecretsMin, Constants.Models.OAuthDownParty.Client.SecretsMax, Constants.Models.SecretHash.SecretLength)]
        [Display(Name = "Secrets")]
        public List<string> Secrets { get; set; }

        public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (!DefaultResourceScope && ResourceScopes.Count <= 0)
            {
                results.Add(new ValidationResult($"The field Resource and scopes must be between 1 and 50 if default Resource Scope is not selected.", new[] { nameof(ResourceScopes) }));
            }
            return results;
        }
    }
}
