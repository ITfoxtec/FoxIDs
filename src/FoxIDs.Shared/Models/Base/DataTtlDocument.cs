using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class DataTtlDocument : DataDocument, IDataTtlDocument
    {
        [Required]
        [JsonProperty(PropertyName = "ttl")]
        public int TimeToLive { get; set; }

        public override string Id { get; set; }
    }
}
