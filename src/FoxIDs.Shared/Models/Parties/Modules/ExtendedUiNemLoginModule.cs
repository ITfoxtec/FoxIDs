using FoxIDs.Models.Modules;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ExtendedUiNemLoginModule
    {
        [Required]
        [JsonProperty(PropertyName = "env")]
        public NemLoginEnvironments Environment { get; set; }
    }
}
