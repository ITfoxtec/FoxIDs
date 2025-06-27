using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.Sequences
{
    public class ExtendedUiStep
    {
        [JsonProperty(PropertyName = "n")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "c")]
        public IEnumerable<ClaimAndValues> Claims { get; set; }
    }
}
