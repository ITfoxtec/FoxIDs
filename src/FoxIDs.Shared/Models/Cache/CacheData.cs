using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class CacheData : DataElement
    {
        [Required]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MaxLength(Constants.Models.DocumentPartitionIdLength)]
        [RegularExpression(Constants.Models.DocumentPartitionIdExPattern)]
        [JsonProperty(PropertyName = "partition_id")]
        public string PartitionId { get; set; }

        [JsonProperty(PropertyName = "data")]
        public string Data { get; set; }
    }
}
