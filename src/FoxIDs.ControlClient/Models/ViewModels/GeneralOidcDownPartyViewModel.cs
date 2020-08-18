using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralOidcDownPartyViewModel : GeneralDownPartyViewModel
    {
        public GeneralOidcDownPartyViewModel() : base(PartyTypes.Oidc)
        { }

        public GeneralOidcDownPartyViewModel(DownParty downParty) : base(downParty)
        { }

        public PageEditForm<OidcDownPartyViewModel> Form { get; set; }

        public SelectUpParty<OidcDownPartyViewModel> SelectAllowUpPartyName;

        public bool EnableClientTab { get; set; } = true;

        public bool EnableResourceTab { get; set; } = true;

        public bool ShowClientTab { get; set; } = true;
    }
}
