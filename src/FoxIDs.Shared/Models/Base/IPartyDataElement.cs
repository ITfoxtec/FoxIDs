namespace FoxIDs.Models
{
    public interface IPartyDataElement : IDataElement
    {
        string Name { get; set; }
        PartyTypes Type { get; set; }
    }
}