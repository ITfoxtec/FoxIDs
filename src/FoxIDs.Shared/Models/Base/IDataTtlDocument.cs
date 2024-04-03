using Newtonsoft.Json;
using System;

namespace FoxIDs.Models
{
    public interface IDataTtlDocument : IDataDocument
    {
        int TimeToLive { get; set; }

        [JsonProperty(PropertyName = "expire_at")]
        public DateTimeOffset ExpireAt => DateTimeOffset.UtcNow.AddSeconds(TimeToLive);
    }
}
