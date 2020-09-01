using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OAuthClientSecretViewModel : OAuthClientSecretResponse
    {
        public bool Removed { get; set; } = false;
    }
}
