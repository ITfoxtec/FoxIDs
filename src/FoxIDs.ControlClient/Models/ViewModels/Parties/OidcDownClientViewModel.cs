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
            ScopesViewModel = new List<OidcDownScopeViewModel>();
            Claims = new List<OidcDownClaim>();
            ResponseTypes = new List<string>();
            ExistingSecrets = new List<OAuthClientSecretViewModel>();
            Secrets = new List<string>();
        }

        [ValidateComplexType]
        [ListLength(0, Constants.Models.OAuthDownParty.Client.ResourceScopesMax)]
        [Display(Name = "Resource and scopes")]
        public new List<OAuthDownResourceScope> ResourceScopes { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.OAuthDownParty.Client.ScopesMin, Constants.Models.OAuthDownParty.Client.ScopesMax)]
        [Display(Name = "Scopes")]
        public List<OidcDownScopeViewModel> ScopesViewModel { get; set; }

        public bool DefaultResourceScope { get; set; } = true;

        public List<string> DefaultResourceScopeScopes { get; set; }

        [Display(Name = "Absolute URIs")]
        public new bool DisableAbsoluteUris { get; set; }

        public List<OAuthClientSecretViewModel> ExistingSecrets { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.OAuthDownParty.Client.SecretsMin, Constants.Models.OAuthDownParty.Client.SecretsMax, Constants.Models.SecretHash.SecretLength)]
        [Display(Name = "Secrets")]
        public List<string> Secrets { get; set; }

        [Display(Name = "Client Credentials grant")]
        public new bool DisableClientCredentialsGrant { get; set; }

        [Display(Name = "Token exchange grant")]
        public new bool DisableTokenExchangeGrant { get; set; }

        [Display(Name = "Client as token exchange actor")]
        public new bool DisableClientAsTokenExchangeActor { get; set; }

        public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (!DefaultResourceScope && ResourceScopes.Count <= 0)
            {
                results.Add(new ValidationResult($"The field Resource and scopes must be between 1 and 50 if default Resource Scope is not selected.", new[] { nameof(ResourceScopes) }));
            }

            var baseResults = base.Validate(validationContext);
            if (baseResults.Count() > 0)
            {
                results.AddRange(baseResults);
            }
            return results;
        }
    }
}
