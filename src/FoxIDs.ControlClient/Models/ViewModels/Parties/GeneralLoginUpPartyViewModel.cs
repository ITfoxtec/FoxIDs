using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralLoginUpPartyViewModel : GeneralUpPartyViewModel
    {
        public GeneralLoginUpPartyViewModel() : base(PartyTypes.Login)
        { }

        public GeneralLoginUpPartyViewModel(UpParty upParty) : base(upParty)
        { }

        public PageEditForm<LoginUpPartyViewModel> Form { get; set; }

        public bool ShowLoginTab { get; set; } = true;
        public bool ShowClaimTransformTab { get; set; } 
        public bool ShowCreateUserTab { get; set; }
        public bool ShowSessionTab { get; set; }
        public bool ShowHrdTab { get; set; }
    }
}
