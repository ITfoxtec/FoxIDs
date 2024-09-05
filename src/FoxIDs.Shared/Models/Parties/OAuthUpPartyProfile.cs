using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class OAuthUpPartyProfile : UpPartyProfile
    {
        [Required]
        [JsonProperty(PropertyName = "client")]
        public OAuthUpClientProfile Client { get; set; }
    }
}
