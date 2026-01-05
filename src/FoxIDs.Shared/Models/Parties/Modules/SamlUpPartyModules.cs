using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class SamlUpPartyModules
    {
        [ValidateComplexType]
        [JsonProperty(PropertyName = "nemlogin")]
        public SamlUpPartyNemLoginModule NemLogin { get; set; }
    }
}

