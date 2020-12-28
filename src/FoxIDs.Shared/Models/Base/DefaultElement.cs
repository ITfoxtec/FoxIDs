using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class DefaultElement : DataElement, IDataDocument
    {
        [Required]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [JsonProperty(PropertyName = "partition_id")]
        public string PartitionId { get; set; }
    }
}
