using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralOidcUpPartyViewModel : GeneralUpPartyViewModel, IGeneralOAuthUpPartyTabViewModel
    {
        public GeneralOidcUpPartyViewModel() : base(PartyTypes.Oidc)
        { }

        public GeneralOidcUpPartyViewModel(UpParty upParty) : base(upParty)
        { }

        public PageEditForm<OidcUpPartyViewModel> Form { get; set; }

        public bool ShowClientTab { get; set; } = true;
        public bool ShowClaimTransformTab { get; set; }
    }
}
