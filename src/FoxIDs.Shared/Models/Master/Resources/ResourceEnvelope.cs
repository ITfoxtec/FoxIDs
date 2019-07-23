using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ResourceEnvelope : MasterDocument
    {
        public static string IdFormat(IdKey idKey) => $"resource:{idKey.Master}";

        [MaxLength(70)]
        [RegularExpression(@"^[\w@:_-]*$")]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Length(0, 20, 5)]
        [JsonProperty(PropertyName = "supported_cultures")]
        public List<string> SupportedCultures { get; set; }        

        [Length(1, 5000)]
        [JsonProperty(PropertyName = "names")]
        public List<ResourceName> Names { get; set; }

        [Length(1, 5000)]
        [JsonProperty(PropertyName = "resources")]
        public List<ResourceItem> Resources { get; set; }
    }
}
