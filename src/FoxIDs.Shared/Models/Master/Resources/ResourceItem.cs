using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ResourceItem
    {
        [Required]
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        [ListLength(Constants.Models.Resource.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        [JsonProperty(PropertyName = "items")]
        public List<ResourceCultureItem> Items { get; set; }
    }
}
