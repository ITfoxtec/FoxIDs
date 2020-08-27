using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralOAuthDownPartyViewModel : GeneralDownPartyViewModel
    {
        public GeneralOAuthDownPartyViewModel() : base(PartyTypes.OAuth2)
        { }

        public GeneralOAuthDownPartyViewModel(DownParty downParty) : base(downParty)
        { }

        public PageEditForm<OAuthDownPartyViewModel> Form { get; set; }

        public SelectUpParty<OAuthDownPartyViewModel> SelectAllowUpPartyName;

        public bool EnableClientTab { get; set; } = true;

        public bool EnableResourceTab { get; set; } = true;

        public bool ShowClientTab { get; set; } = true;
    }
}
