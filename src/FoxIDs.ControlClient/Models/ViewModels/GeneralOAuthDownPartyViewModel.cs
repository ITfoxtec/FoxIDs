using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralOAuthDownPartyViewModel : GeneralDownPartyViewModel
    {
        public GeneralOAuthDownPartyViewModel() : base(PartyTypes.OAuth2)
        { }

        public GeneralOAuthDownPartyViewModel(DownParty downParty) : base(downParty)
        { }

        public PageEditForm<OAuthDownPartyViewModel> Form { get; set; }

        public bool ShowClientTab { get; set; } = true;
    }
}
