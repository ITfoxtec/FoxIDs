namespace FoxIDs.Models
{
    public class UiUpPartyData : DataDocument, IUiUpParty
    {
        public override string Id { get; set; }

        public string CssStyle { get; set; }
    }
}
