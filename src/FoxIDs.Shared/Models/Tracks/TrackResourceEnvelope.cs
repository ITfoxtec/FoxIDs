using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models
{
    public class TrackResourceEnvelope
    {
        [ListLength(Constants.Models.Track.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        [JsonProperty(PropertyName = "names")]
        public List<ResourceName> Names { get; set; }

        [ListLength(Constants.Models.Track.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        [JsonProperty(PropertyName = "resources")]
        public List<ResourceItem> Resources { get; set; }
    }
}
