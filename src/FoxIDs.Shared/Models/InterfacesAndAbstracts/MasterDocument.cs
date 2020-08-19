using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public abstract class MasterDocument : DataElement, IDataDocument
    {
        public static string PartitionIdFormat(IdKey idKey) => $"{idKey.Master}";

        [Required]
        [MaxLength(Constants.Models.MasterPartitionIdLength)]
        [RegularExpression(Constants.Models.MasterPartitionIdExPattern)]
        [JsonProperty(PropertyName = "partition_id")]
        public string PartitionId { get; set; }

        public class IdKey
        {
            [Required]
            [MaxLength(Constants.Models.Master.IdLength)]
            [RegularExpression(Constants.Models.Master.IdRegExPattern)]
            public string Master => "@master";
        }
    }
}
