using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OidcDownClientViewModel : OidcDownClient
    {
        public OidcDownClientViewModel()
        {
            ResponseTypes = new List<string> { "code" };
        }

        [Length(Constants.Models.OAuthDownParty.Client.SecretsMin, Constants.Models.OAuthDownParty.Client.SecretsMax)]
        public List<OAuthClientSecretRequest> Secrets { get; set; }
    }
}
