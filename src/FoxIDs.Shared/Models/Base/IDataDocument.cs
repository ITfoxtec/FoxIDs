namespace FoxIDs.Models
{
    public interface IDataDocument : IDataElement
    {
        string PartitionId { get; set; }
    }
}
