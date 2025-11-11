using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class TrackResourceLarge : DataDocument
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) throw new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.TrackResourceLarge}:{idKey.TenantName}:{idKey.TrackName}:{idKey.UniqueId}";
        }

        [Required]
        [MaxLength(Constants.Models.Resource.ResourceLarge.IdLength)]
        [RegularExpression(Constants.Models.Resource.ResourceLarge.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MaxLength(Constants.Models.Resource.NameLength)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [ListLength(Constants.Models.Resource.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        [JsonProperty(PropertyName = "items")]
        public List<TrackResourceLargeCultureItem> Items { get; set; }

        public class IdKey : Track.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.Resource.ResourceLarge.UniqueIdLength)]
            [RegularExpression(Constants.Models.Resource.ResourceLarge.UniqueIdRegExPattern)]
            public string UniqueId { get; set; }
        }
    }
}
