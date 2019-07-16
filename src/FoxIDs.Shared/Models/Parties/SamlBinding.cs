using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class SamlBinding
    {
        [Required]
        [MaxLength(30)]
        [JsonProperty(PropertyName = "request_binding")]
        public string RequestBinding { get; set; }

        [Required]
        [MaxLength(30)]
        [JsonProperty(PropertyName = "response_binding")]
        public string ResponseBinding { get; set; }
    }
}
