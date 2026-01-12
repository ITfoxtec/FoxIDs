using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ExtendedUiModules
    {
        [ValidateComplexType]
        [JsonProperty(PropertyName = "nemlogin")]
        public ExtendedUiNemLoginModule NemLogin { get; set; }
    }
}
