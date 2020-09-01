using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralUpPartyViewModel : UpParty
    {
        public GeneralUpPartyViewModel(PartyTypes type)
        {
            Type = type;
        }

        public GeneralUpPartyViewModel(UpParty upParty)
        {
            Name = upParty.Name;
            Type = upParty.Type;
        }

        public bool Edit { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool CreateMode { get; set; }

        public bool DeleteAcknowledge { get; set; }

        public string Error { get; set; }
    }
}
