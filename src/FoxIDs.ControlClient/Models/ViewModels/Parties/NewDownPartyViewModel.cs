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

        public string AppTitle { get; set; }

        public bool ShowAll { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool CreateWorking { get; set; }

        public bool Created { get; set; }

        public PageEditForm<NewDownPartyOidcViewModel> OidcForm { get; set; }
        
        public PageEditForm<NewDownPartyOAuthClientViewModel> OAuthClientForm { get; set; }

        public PageEditForm<NewDownPartyOAuthResourceViewModel> OAuthResourceForm { get; set; }

        public PageEditForm<NewDownPartySamlViewModel> SamlForm { get; set; }

        public void Init()
        {
            AppTitle = null;
            Type = null;
            OAuthType = null;
            ShowAll = false;
            ShowAdvanced = false;
            CreateWorking = false;
            Created = false;
            OidcForm = new PageEditForm<NewDownPartyOidcViewModel>();
            OAuthClientForm = new PageEditForm<NewDownPartyOAuthClientViewModel>();
            OAuthResourceForm = new PageEditForm<NewDownPartyOAuthResourceViewModel>();
            SamlForm = new PageEditForm<NewDownPartySamlViewModel>();
        }
    }
}
