namespace FoxIDs.Models
{
    public class CacheTtlData : CacheData, IDataTtlDocument
    {
        public int TimeToLive { get; set; }
    }
}
