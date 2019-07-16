using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public abstract class MasterDocument : DataElement, IDataDocument
    {
        public static string PartitionIdFormat(IdKey idKey) => $"{idKey.Master}";

        [Required]
        [MaxLength(10)]
        [RegularExpression(@"^[\w@]*$")]
        [JsonProperty(PropertyName = "partition_id")]
        public string PartitionId { get; set; }

        public class IdKey
        {
            [Required]
            [MaxLength(10)]
            [RegularExpression(@"^[\w@]*$")]
            public string Master => "@master";
        }
    }
}
