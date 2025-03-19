namespace FoxIDs.Models.Logic
{
    public class IdPInitiatedDownPartyLink
    {
        public string UpPartyName { get; set; }

        public PartyTypes UpPartyType { get; set; }

        public string DownPartyId { get; set; }

        public PartyTypes DownPartyType { get; set; }

        public string DownPartyRedirectUrl { get; set; }
    }
}
