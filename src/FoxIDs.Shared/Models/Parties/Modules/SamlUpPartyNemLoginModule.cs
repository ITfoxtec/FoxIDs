using FoxIDs.Models.Modules;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class SamlUpPartyNemLoginModule
    {
        [Required]
        [JsonProperty(PropertyName = "env")]
        public NemLoginEnvironments Environment { get; set; }

        [Required]
        [JsonProperty(PropertyName = "sector")]
        public NemLoginSectors Sector { get; set; }

        [JsonProperty(PropertyName = "req_cpr")]
        public bool RequestCpr { get; set; }

        [JsonProperty(PropertyName = "save_cpr_ext_user")]
        public bool SaveCprOnExternalUsers { get; set; }
    }
}

