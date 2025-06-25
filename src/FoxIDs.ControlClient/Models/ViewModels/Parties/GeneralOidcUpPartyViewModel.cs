using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralOidcUpPartyViewModel : GeneralUpPartyViewModel, IGeneralOAuthUpPartyTabViewModel
    {
        public GeneralOidcUpPartyViewModel() : base(PartyTypes.Oidc)
        { }

        public GeneralOidcUpPartyViewModel(UpParty upParty) : base(upParty)
        { }

        public PageEditForm<OidcUpPartyViewModel> Form { get; set; }

        public List<KeyInfoViewModel> KeyInfoList { get; set; } = new List<KeyInfoViewModel>();

        public bool ShowClientTab { get; set; } = true;
        public bool ShowClaimTransformTab { get; set; }
        public bool ShowExtendedUiTab { get; set; }
        public bool ShowLinkExternalUserTab { get; set; }
        public bool ShowHrdTab { get; set; }
        public bool ShowProfileTab { get; set; }
        public bool ShowSessionTab { get; set; }
    }
}
