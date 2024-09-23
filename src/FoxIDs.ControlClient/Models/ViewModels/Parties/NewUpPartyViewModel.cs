using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public class NewUpPartyViewModel 
    {
        public Modal Modal;

        public PartyTypes? Type { get; set; }

        public IdPTypes? IdPType { get; set; }

        public string AppTitle { get; set; }

        public bool ShowAll { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool CreateWorking { get; set; }

        public bool Created { get; set; }

        public IEnumerable<Track> SelectTracks { get; set; }

        public PageEditForm<FilterTrackViewModel> SelectTrackFilterForm { get; set; }

        public PageEditForm<NewUpPartyEnvironmentLinkViewModel> EnvironmentLinkForm { get; set; }

        public PageEditForm<NewUpPartyNemLoginViewModel> NemLoginForm { get; set; }

        //public PageEditForm<NewUpPartyOidcViewModel> OidcForm { get; set; }

        //public PageEditForm<NewUpPartyEnvironmentLinkViewModel> OAuthTokenExchangeForm { get; set; }

        //public PageEditForm<NewUpPartySamlViewModel> SamlForm { get; set; }

        public void Init()
        {
            AppTitle = null;
            Type = null;
            IdPType = null;
            ShowAll = false;
            ShowAdvanced = false;
            CreateWorking = false;
            Created = false;
            SelectTrackFilterForm = new PageEditForm<FilterTrackViewModel>();
            EnvironmentLinkForm = new PageEditForm<NewUpPartyEnvironmentLinkViewModel>();
            //OidcForm = new PageEditForm<NewUpPartyOidcViewModel>();
            //OAuthTokenExchangeForm = new PageEditForm<NewUpPartyEnvironmentLinkViewModel>();
            //SamlForm = new PageEditForm<NewUpPartySamlViewModel>();
        }
    }
}
