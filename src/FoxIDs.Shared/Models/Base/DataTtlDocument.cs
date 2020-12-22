using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public abstract class DataTtlDocument : DataDocument, IDataTtlDocument
    {
        [Required]
        [JsonProperty(PropertyName = "ttl")]
        public int TimeToLive { get; set; }
    }
}
