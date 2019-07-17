using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.Resources
{
    public class EmbeddedResource
    {
        [Length(0, 20, 5)]
        [JsonProperty(PropertyName = "supported_cultures")]
        public List<string> SupportedCultures { get; set; }        

        [Length(1, 5000)]
        [JsonProperty(PropertyName = "names")]
        public List<ResourceName> Names { get; set; }

        [Length(1, 5000)]
        [JsonProperty(PropertyName = "resources")]
        public List<Resource> Resources { get; set; }
    }
}
