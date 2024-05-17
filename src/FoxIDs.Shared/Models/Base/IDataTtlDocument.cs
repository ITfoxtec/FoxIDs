using System;

namespace FoxIDs.Models
{
    public interface IDataTtlDocument : IDataDocument
    {
        int TimeToLive { get; set; }

        DateTime ExpireAt { get; set; }
    }
}
