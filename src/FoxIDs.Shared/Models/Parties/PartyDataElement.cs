using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class PartyDataElement : DataElement
    {
        [MaxLength(110)]
        [RegularExpression(@"^[\w:_-]*$")]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MaxLength(30)]
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonIgnore]
        public string Name { get => Id.Substring(Id.LastIndexOf(':') + 1); }
    }
}