using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Session
{
    public class UpPartySessionLink
    {
        [Required]
        [MaxLength(Constants.Models.Party.IdLength)]
        [RegularExpression(Constants.Models.Party.IdRegExPattern)]
        [JsonProperty(PropertyName = "i")]
        public string Id { get; set; }

        [Required]
        [JsonProperty(PropertyName = "t")]
        public PartyTypes Type { get; set; }
    }
}
