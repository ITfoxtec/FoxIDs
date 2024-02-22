using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralDownPartyViewModel : DownParty
    {
        public GeneralDownPartyViewModel(PartyTypes type)
        {
            Type = type;
        }

        public GeneralDownPartyViewModel(DownParty downParty)
        {
            Name = downParty.Name;
            DisplayName = downParty.DisplayName;
            Type = downParty.Type;
        }

        public bool Edit { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool CreateMode { get; set; }

        public bool DeleteAcknowledge { get; set; }

        public string Error { get; set; }
    }
}
