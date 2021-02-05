using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralOidcDownPartyViewModel : GeneralDownPartyViewModel, IGeneralOAuthDownPartyTabViewModel
    {
        public GeneralOidcDownPartyViewModel() : base(PartyTypes.Oidc)
        { }

        public GeneralOidcDownPartyViewModel(DownParty downParty) : base(downParty)
        { }

        public PageEditForm<OidcDownPartyViewModel> Form { get; set; }

        public SelectUpParty<OidcDownPartyViewModel> SelectAllowUpPartyName;

        public bool EnableClientTab { get; set; } = true;

        public bool EnableResourceTab { get; set; }

        public bool ShowClientTab { get; set; } = true;
        public bool ShowResourceTab { get; set; }
        public bool ShowClaimTransformTab { get; set; }
    }
}
