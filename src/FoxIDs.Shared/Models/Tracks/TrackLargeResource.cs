using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class TrackLargeResource : DataDocument
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) throw new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.TrackLargeResource}:{idKey.TenantName}:{idKey.TrackName}:{idKey.UniqueId}";
        }

        [Required]
        [MaxLength(Constants.Models.Resource.LargeResource.IdLength)]
        [RegularExpression(Constants.Models.Resource.LargeResource.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MaxLength(Constants.Models.Resource.NameLength)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [ListLength(Constants.Models.Resource.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        [JsonProperty(PropertyName = "items")]
        public List<TrackLargeResourceCultureItem> Items { get; set; }

        public class IdKey : Track.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.Resource.LargeResource.UniqueIdLength)]
            [RegularExpression(Constants.Models.Resource.LargeResource.UniqueIdRegExPattern)]
            public string UniqueId { get; set; }
        }
    }
}
