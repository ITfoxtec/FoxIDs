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
        public static string IdFormat(IdKey idKey) => $"track:{idKey.TenantName}:{idKey.TrackName}";

        [Required]
        [MaxLength(80)]
        [RegularExpression(@"^[\w:_-]*$")]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [JsonProperty(PropertyName = "primary_key")]
        public TrackKey PrimaryKey { get; set; }

        [JsonProperty(PropertyName = "secondary_key")]
        public TrackKey SecondaryKey { get; set; }

        [JsonProperty(PropertyName = "claim_mappings")]
        public ClaimMappingsDataElement ClaimMappings { get; set; }

        [Length(0, 5000)]
        [JsonProperty(PropertyName = "resources")]
        public List<ResourceItem> Resources { get; set; }

        [Range(30, 1800)] // 30 seconds to 30 minutes 
        [JsonProperty(PropertyName = "sequence_lifetime")] 
        public int SequenceLifetime { get; set; }

        [Range(6, 20)]
        [JsonProperty(PropertyName = "password_length")]
        public int PasswordLength { get; set; }

        [Required]
        [JsonProperty(PropertyName = "check_password_complexity")]
        public bool? CheckPasswordComplexity { get; set; }

        [Required]
        [JsonProperty(PropertyName = "check_password_risk")]
        public bool? CheckPasswordRisk { get; set; }

        [JsonIgnore]
        public string Name
        {
            get
            {
                return Id.Substring(Id.LastIndexOf(':') + 1);
            }
        }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            Id = IdFormat(idKey);
        }

        public class IdKey : Tenant.IdKey
        {
            [Required]
            [MaxLength(30)]
            [RegularExpression(@"^[\w-_]*$")]
            public string TrackName { get; set; }
        }
    }
}
