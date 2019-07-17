using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public abstract class DataDocument : DataElement, IDataDocument
    {
        public static string PartitionIdFormat(Track.IdKey idKey) => $"{idKey.TenantName}:{idKey.TrackName}";
        
        [Required]
        [MaxLength(70)]
        [RegularExpression(@"^[\w:_-]*$")]
        [JsonProperty(PropertyName = "partition_id")]
        public string PartitionId { get; set; }
    }
}
