using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.Logic
{
    public class DkPrivilegeGroup
    {
        [JsonProperty(PropertyName = "cvr")]
        public string CvrNumber { get; set; }

        [JsonProperty(PropertyName = "pu")]
        public string ProductionUnit { get; set; }

        [JsonProperty(PropertyName = "se")]
        public string SeNumber { get; set; }

        [JsonProperty(PropertyName = "cpr")]
        public string CprNumber { get; set; }

        [JsonProperty(PropertyName = "c")]
        public Dictionary<string, string> Constraint { get; set; }

        [JsonProperty(PropertyName = "p")]
        public List<string> Privilege { get; set; } = new List<string>();
    }
}
