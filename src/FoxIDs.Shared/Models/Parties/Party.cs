using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class Party : PartyDataElement, IDataDocument
    {
        [Required]
        [MaxLength(70)]
        [RegularExpression(@"^[\w:_-]*$")]
        [JsonProperty(PropertyName = "partition_id")]
        public string PartitionId { get; set; }

        public class IdKey : Track.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.PartyNameLength)]
            [RegularExpression(@"^[\w-_]*$")]
            public string PartyName { get; set; }
        }
    }
}