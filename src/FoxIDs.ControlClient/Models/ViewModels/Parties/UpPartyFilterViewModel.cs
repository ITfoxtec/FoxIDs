using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class UpPartyFilterViewModel 
    {
        public bool Selected { get; set; }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string ProfileName { get; set; }

        public string ProfileDisplayName { get; set; }

        public PartyTypes Type { get; set; }

        public string TypeText { get; set; }
    }
}
