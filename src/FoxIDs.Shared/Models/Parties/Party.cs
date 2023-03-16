using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class Party : PartyDataElement, IDataDocument
    {
        [Required]
        [MaxLength(Constants.Models.DocumentPartitionIdLength)]
        [RegularExpression(Constants.Models.DocumentPartitionIdExPattern)]
        [JsonProperty(PropertyName = "partition_id")]
        public string PartitionId { get; set; }

        public class IdKey : Track.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.Party.NameLength)]
            [RegularExpression(Constants.Models.Party.NameRegExPattern)]
            public string PartyName { get; set; }
        }

        [MaxLength(Constants.Models.Party.NoteLength)]
        [JsonProperty(PropertyName = "note")]
        public string Note { get; set; }
    }
}