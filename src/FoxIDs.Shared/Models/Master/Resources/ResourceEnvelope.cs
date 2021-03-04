using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models
{
    public class ResourceEnvelope 
    {
        [Length(Constants.Models.Resource.SupportedCulturesMin, Constants.Models.Resource.SupportedCulturesMax, Constants.Models.Resource.SupportedCulturesLength)]
        [JsonProperty(PropertyName = "supported_cultures")]
        public List<string> SupportedCultures { get; set; }        

        [Length(Constants.Models.Resource.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        [JsonProperty(PropertyName = "names")]
        public List<ResourceName> Names { get; set; }

        [Length(Constants.Models.Resource.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        [JsonProperty(PropertyName = "resources")]
        public List<ResourceItem> Resources { get; set; }
    }
}
