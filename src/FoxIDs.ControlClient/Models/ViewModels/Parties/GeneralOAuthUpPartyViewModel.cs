using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralOAuthUpPartyViewModel : GeneralUpPartyViewModel, IGeneralOAuthUpPartyTabViewModel
    {
        public GeneralOAuthUpPartyViewModel() : base(PartyTypes.OAuth2)
        { }

        public GeneralOAuthUpPartyViewModel(UpParty upParty) : base(upParty)
        { }

        public PageEditForm<OAuthUpPartyViewModel> Form { get; set; }

        public List<KeyInfoViewModel> KeyInfoList { get; set; } = new List<KeyInfoViewModel>();

        public bool ShowClientTab { get; set; } = true;
        public bool ShowClaimTransformTab { get; set; }
        public bool ShowLinkExternalUserTab { get; set; }
        public bool ShowHrdTab { get; set; }
        public bool ShowProfileTab { get; set; }
        public bool ShowSessionTab { get; set; }
    }
}
