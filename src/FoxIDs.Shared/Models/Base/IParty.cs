namespace FoxIDs.Models
{
    public interface IParty : IPartyDataElement
    {
        string PartitionId { get; set; }
        string DisplayName { get; set; }
        string Note { get; set; }
    }
}