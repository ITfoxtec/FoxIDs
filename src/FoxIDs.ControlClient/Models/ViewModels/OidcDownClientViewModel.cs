using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OidcDownClientViewModel : OidcDownClient
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

        public bool DefaultResourceScope { get; set; } = true;

        [Display(Name = "Scopes")]
        public List<string> DefaultResourceScopeScopes { get; set; }

        public List<OAuthClientSecretViewModel> ExistingSecrets { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.OAuthDownParty.Client.SecretsMin, Constants.Models.OAuthDownParty.Client.SecretsMax, Constants.Models.SecretHash.SecretLength)]
        [Display(Name = "Secrets")]
        public List<string> Secrets { get; set; }
    }
}
