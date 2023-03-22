using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralTrackLinkDownPartyViewModel : GeneralDownPartyViewModel
    {
        public GeneralTrackLinkDownPartyViewModel() : base(PartyTypes.TrackLink)
        { }

        public GeneralTrackLinkDownPartyViewModel(DownParty downParty) : base(downParty)
        { }

        public PageEditForm<TrackLinkDownPartyViewModel> Form { get; set; }

        public SelectUpParty<TrackLinkDownPartyViewModel> SelectAllowUpPartyName;

        public bool ShowTrackLinkTab { get; set; } = true;
        public bool ShowClaimTransformTab { get; set; }
    }
}
