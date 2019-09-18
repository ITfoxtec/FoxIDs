using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class PartyDataElement : DataElement
    {
        [MaxLength(Constants.Models.OAuthParty.IdLength)]
        [RegularExpression(Constants.Models.OAuthParty.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MaxLength(Constants.Models.OAuthParty.TypeLength)]
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonIgnore]
        public string Name { get => Id.Substring(Id.LastIndexOf(':') + 1); }
    }
}