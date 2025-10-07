using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class NewDownPartyViewModel 
    {
        public bool IsVisible { get; set; }

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

        public GeneralDownPartyViewModel CreatedDownParty { get; set; }

        public bool ShowOidcAuthorityDetails { get; set; }

        public bool ShowOAuthClientAuthorityDetails { get; set; }

        public bool ShowOAuthResourceAuthorityDetails { get; set; }

        public void Init()
        {
            IsVisible = false;
            AppTitle = null;
            Type = null;
            OAuthType = null;
            OAuthClientType = null;
            ShowAll = false;
            ShowAdvanced = false;
            CreateWorking = false;
            Created = false;
            CreatedDownParty = null;
            ShowOidcAuthorityDetails = false;
            ShowOAuthClientAuthorityDetails = false;
            ShowOAuthResourceAuthorityDetails = false;
            OidcForm = new PageEditForm<NewDownPartyOidcViewModel>();
            OAuthClientForm = new PageEditForm<NewDownPartyOAuthClientViewModel>();
            OAuthResourceForm = new PageEditForm<NewDownPartyOAuthResourceViewModel>();
            SamlForm = new PageEditForm<NewDownPartySamlViewModel>();
        }
    }
}
