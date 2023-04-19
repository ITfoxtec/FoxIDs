using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralTrackLinkUpPartyViewModel : GeneralUpPartyViewModel
    {
        public GeneralTrackLinkUpPartyViewModel() : base(PartyTypes.TrackLink)
        { }

        public GeneralTrackLinkUpPartyViewModel(UpParty upParty) : base(upParty)
        { }

        public PageEditForm<TrackLinkUpPartyViewModel> Form { get; set; }

        public List<KeyInfoViewModel> KeyInfoList { get; set; } = new List<KeyInfoViewModel>();

        public bool ShowTrackLinkTab { get; set; } = true;
        public bool ShowSessionTab { get; set; }
        public bool ShowClaimTransformTab { get; set; }
        public bool ShowHrdTab { get; set; }
    }
}
