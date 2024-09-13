using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralExternalLoginUpPartyViewModel : GeneralUpPartyViewModel
    {
        public GeneralExternalLoginUpPartyViewModel() : base(PartyTypes.ExternalLogin)
        { }

        public GeneralExternalLoginUpPartyViewModel(UpParty upParty) : base(upParty)
        { }

        public PageEditForm<ExternalLoginUpPartyViewModel> Form { get; set; }

        public bool ShowExternalLoginTab { get; set; } = true;
        public bool ShowClaimTransformTab { get; set; }
        public bool ShowLinkExternalUserTab { get; set; }        
        public bool ShowHrdTab { get; set; }
        public bool ShowProfileTab { get; set; }
        public bool ShowSessionTab { get; set; }
    }
}
