using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralOAuthDownPartyViewModel : GeneralDownPartyViewModel, IGeneralOAuthDownPartyTabViewModel
    {
        public GeneralOAuthDownPartyViewModel() : base(PartyTypes.OAuth2)
        { }

        public GeneralOAuthDownPartyViewModel(DownParty downParty) : base(downParty)
        { }

        public PageEditForm<OAuthDownPartyViewModel> Form { get; set; }

        public SelectUpParty<OAuthDownPartyViewModel> SelectAllowUpPartyName;

        public bool EnableClientTab { get; set; } = false;

        public bool EnableResourceTab { get; set; } = true;

        public bool ShowClientTab { get; set; }
        public bool ShowResourceTab { get; set; } = true;
        public bool ShowClaimTransformTab { get; set; }
    }
}
