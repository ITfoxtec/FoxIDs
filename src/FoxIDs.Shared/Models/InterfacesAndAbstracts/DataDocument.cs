using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public abstract class DataDocument : DataElement, IDataDocument
    {
        public static string PartitionIdFormat(Track.IdKey idKey) => $"{idKey.TenantName}:{idKey.TrackName}";
        
        [Required]
        [MaxLength(Constants.Models.DocumentPartitionIdLength)]
        [RegularExpression(Constants.Models.DocumentPartitionIdExPattern)]
        [JsonProperty(PropertyName = "partition_id")]
        public string PartitionId { get; set; }
    }
}
