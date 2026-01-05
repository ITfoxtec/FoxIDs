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
            DisplayName = upParty.DisplayName;
            Type = upParty.Type;
            ModuleType = upParty.ModuleType;
            Profiles = upParty.Profiles;
        }

        public bool TokenExchange { get; set; }

        public bool Edit { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool CreateMode { get; set; }

        public bool DeleteAcknowledge { get; set; }

        public string Error { get; set; }
    }
}
