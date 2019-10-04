using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class PartyDataElement : DataElement
    {
        [MaxLength(Constants.Models.PartyIdLength)]
        [RegularExpression(Constants.Models.PartyIdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [JsonProperty(PropertyName = "type")]
        public PartyType Type { get; set; }

        [JsonIgnore]
        public string Name { get => Id.Substring(Id.LastIndexOf(':') + 1); }
    }
}