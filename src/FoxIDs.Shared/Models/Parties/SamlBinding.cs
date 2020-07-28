using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class SamlBinding
    {
        [Required]
        [JsonProperty(PropertyName = "request_binding")]
        public SamlBindingTypes RequestBinding { get; set; }

        [Required]
        [JsonProperty(PropertyName = "response_binding")]
        public SamlBindingTypes ResponseBinding { get; set; }
    }
}
