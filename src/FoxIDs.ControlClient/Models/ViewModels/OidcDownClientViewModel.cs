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
            ResourceScopes = new List<OAuthDownResourceScope>();
            Scopes = new List<OidcDownScope>();
            Claims = new List<OidcDownClaim>();
            ResponseTypes = new List<string> { "code" };
            ExistingSecrets = new List<OAuthClientSecretViewModel>();
            Secrets = new List<string>();
        }

        public List<OAuthClientSecretViewModel> ExistingSecrets { get; set; }

        [Length(Constants.Models.OAuthDownParty.Client.SecretsMin, Constants.Models.OAuthDownParty.Client.SecretsMax, Constants.Models.SecretHash.SecretLength)]
        [Display(Name = "Secrets")]
        public List<string> Secrets { get; set; }
    }
}
