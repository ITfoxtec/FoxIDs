using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class UpPartyLink
    {
        //[RegularExpression(Constants.Models.PartyIdRegExPattern)]
        //[JsonProperty(PropertyName = "id")]
        //public override string Id { get; set; }

        [Required]
        [MaxLength(Constants.Models.PartyNameLength)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [Required]
        [JsonProperty(PropertyName = "type")]
        public PartyType Type { get; set; }

    }
}
