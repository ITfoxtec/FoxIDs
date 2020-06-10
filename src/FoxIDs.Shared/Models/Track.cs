using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class Track : DataDocument
    {
        public static async Task<string> IdFormat(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"track:{idKey.TenantName}:{idKey.TrackName}";
        }

        public static async Task<string> IdFormat(RouteBinding routeBinding, string name)
        {
            if (routeBinding == null) new ArgumentNullException(nameof(routeBinding));
            if (name == null) new ArgumentNullException(nameof(name));

            var idKey = new IdKey
            {
                TenantName = routeBinding.TenantName,
                TrackName = routeBinding.TrackName,
            };

            return await IdFormat(idKey);
        }

        public static new string PartitionIdFormat(IdKey idKey) => $"{idKey.TenantName}:tracks";

        [Required]
        [MaxLength(Constants.Models.Track.IdLength)]
        [RegularExpression(Constants.Models.Track.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [JsonProperty(PropertyName = "primary_key")]
        public TrackKey PrimaryKey { get; set; }

        [JsonProperty(PropertyName = "secondary_key")]
        public TrackKey SecondaryKey { get; set; }

        [JsonProperty(PropertyName = "claim_mappings")]
        public ClaimMappingsDataElement ClaimMappings { get; set; }

        [Length(Constants.Models.Track.ResourcesMin, Constants.Models.Track.ResourcesMax)]
        [JsonProperty(PropertyName = "resources")]
        public List<ResourceItem> Resources { get; set; }

        [Range(Constants.Models.Track.SequenceLifetimeMin, Constants.Models.Track.SequenceLifetimeMax)] // 30 seconds to 3 hours
        [JsonProperty(PropertyName = "sequence_lifetime")] 
        public int SequenceLifetime { get; set; }

        [Range(Constants.Models.Track.PasswordLengthMin, Constants.Models.Track.PasswordLengthMax)]
        [JsonProperty(PropertyName = "password_length")]
        public int PasswordLength { get; set; }

        [Required]
        [JsonProperty(PropertyName = "check_password_complexity")]
        public bool? CheckPasswordComplexity { get; set; }

        [Required]
        [JsonProperty(PropertyName = "check_password_risk")]
        public bool? CheckPasswordRisk { get; set; }

        [Length(Constants.Models.Track.AllowIframeOnDomainsMin, Constants.Models.Track.AllowIframeOnDomainsMax, Constants.Models.Track.AllowIframeOnDomainsLength)]
        [JsonProperty(PropertyName = "allow_iframe_on_domains")]
        public List<string> AllowIframeOnDomains { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormat(idKey);
            Name = Id.Substring(Id.LastIndexOf(':') + 1); ;
        }

        public class IdKey : Tenant.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.Track.NameLength)]
            [RegularExpression(Constants.Models.Track.NameRegExPattern)]
            public string TrackName { get; set; }
        }
    }
}
