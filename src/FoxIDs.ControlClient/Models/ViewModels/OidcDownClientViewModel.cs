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
        }

        [ValidateComplexType]
        [Length(Constants.Models.OAuthDownParty.Client.SecretsMin, Constants.Models.OAuthDownParty.Client.SecretsMax)]
        [Display(Name = "Secrets")]
        public List<OAuthClientSecretRequest> Secrets { get; set; }
    }
}
