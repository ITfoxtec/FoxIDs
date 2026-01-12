using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class SamlUpPartyModules
    {
        [JsonProperty(PropertyName = "show_std_settings")]
        public bool ShowStandardSettings { get; set; }

        [ValidateComplexType]
        [JsonProperty(PropertyName = "nemlogin")]
        public SamlUpPartyNemLoginModule NemLogin { get; set; }
    }
}