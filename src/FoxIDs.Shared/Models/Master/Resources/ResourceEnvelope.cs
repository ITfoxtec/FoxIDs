using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ResourceEnvelope : MasterDocument
    {
        public static string IdFormat(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            return $"resource:{idKey.Master}";
        }

        [MaxLength(Constants.Models.Resource.EnvelopeIdLength)]
        [RegularExpression(Constants.Models.Resource.EnvelopeIdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

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
