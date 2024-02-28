using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class NewDownPartyViewModel 
    {
        public Modal Modal;

        public PartyTypes? Type { get; set; }

        public DownPartyOAuthTypes? OAuthType { get; set; }

        public DownPartyOAuthClientTypes? OAuthClientType { get; set; }

        public bool ShowAll { get; set; }

        public bool ShowAdvanced { get; set; }

        public PageEditForm<NewDownPartyOidcViewModel> OidcForm { get; set; }

        public void Init()
        {
            Type = null;
            OAuthType = null;
            ShowAll = false;
            ShowAdvanced = false;
            OidcForm = new PageEditForm<NewDownPartyOidcViewModel>();
        }
    }
}
