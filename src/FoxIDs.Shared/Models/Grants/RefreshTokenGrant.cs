using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class RefreshTokenGrant : DataDocument
    {
        public static async Task<string> IdFormat(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"rtgrant:{idKey.TenantName}:{idKey.TrackName}:{idKey.RefreshToken}";
        }

        [Required]
        [MaxLength(180)]
        [RegularExpression(@"^[\w:\-_]*$")]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [JsonIgnore]
        public string RefreshToken { get => Id.Substring(Id.LastIndexOf(':') + 1); }

        [Length(1, 1000)]
        [JsonProperty(PropertyName = "claims")]
        public List<ClaimAndValues> Claims { get; set; }

        [MaxLength(30)]
        [JsonProperty(PropertyName = "client_id")]
        public string ClientId { get; set; }

        [MaxLength(150)]
        [JsonProperty(PropertyName = "scope")]
        public string Scope { get; set; }

        [MaxLength(100)]
        [JsonProperty(PropertyName = "session_id")]
        public string SessionId { get; set; }

        [Required]
        [JsonProperty(PropertyName = "ct")]
        public long CreateTime { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormat(idKey);
        }

        public class IdKey : Track.IdKey
        {
            [Required]
            [MaxLength(100)]
            [RegularExpression(@"^[\w-_]*$")]
            public string RefreshToken { get; set; }
        }
    }
}
