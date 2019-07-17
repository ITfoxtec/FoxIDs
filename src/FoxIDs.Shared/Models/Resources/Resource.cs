using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Resources
{
    public class Resource
    {
        [Required]
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        [Length(1, 5000)]
        [JsonProperty(PropertyName = "items")]
        public List<ResourceItem> Items { get; set; }
    }
}
