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

            return $"{Constants.Models.DataType.TrackLargeResource}:{idKey.TenantName}:{idKey.TrackName}:{idKey.Name}";
        }

        public static async Task<string> IdFormatAsync(RouteBinding routeBinding, string name)
        {
            if (routeBinding == null) new ArgumentNullException(nameof(routeBinding));
            if (name == null) new ArgumentNullException(nameof(name));

            var idKey = new IdKey
            {
                TenantName = routeBinding.TenantName,
                TrackName = routeBinding.TrackName,
                Name = name,
            };

            return await IdFormatAsync(idKey);
        }

        [Required]
        [MaxLength(Constants.Models.Resource.LargeResource.IdLength)]
        [RegularExpression(Constants.Models.Resource.LargeResource.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MinLength(Constants.Models.Resource.LargeResource.NameMinLength)]
        [MaxLength(Constants.Models.Resource.LargeResource.NameMaxLength)]
        [RegularExpression(Constants.Models.Resource.LargeResource.NameRegExPattern)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [ListLength(Constants.Models.Resource.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        [JsonProperty(PropertyName = "items")]
        public List<TrackLargeResourceCultureItem> Items { get; set; }

        public class IdKey : Track.IdKey
        {
            [Required]
            [MinLength(Constants.Models.Resource.LargeResource.NameMinLength)]
            [MaxLength(Constants.Models.Resource.LargeResource.NameMaxLength)]
            [RegularExpression(Constants.Models.Resource.LargeResource.NameRegExPattern)]
            public string Name { get; set; }
        }
    }
}
