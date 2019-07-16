using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class AuthCodeTtlGrant : DataTtlDocument
    {
        public static string IdFormat(IdKey idKey) => $"acgrant:{idKey.TenantName}:{idKey.TrackName}:{idKey.Code}";

        [Required]
        [MaxLength(180)]
        [RegularExpression(@"^[\w:\-_]*$")]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Length(1, 1000)]
        [JsonProperty(PropertyName = "claims")]
        public List<ClaimAndValues> Claims { get; set; }

        [MaxLength(30)]
        [JsonProperty(PropertyName = "client_id")]
        public string ClientId { get; set; }

        [MaxLength(500)]
        [JsonProperty(PropertyName = "redirect_uri")]
        public string RedirectUri { get; set; }

        [MaxLength(150)]
        [JsonProperty(PropertyName = "scope")]
        public string Scope { get; set; }

        [MaxLength(550)]
        [JsonProperty(PropertyName = "nonce")]
        public string Nonce { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            Id = IdFormat(idKey);
        }

        public class IdKey : Track.IdKey
        {
            [Required]
            [MaxLength(100)]
            [RegularExpression(@"^[\w-_]*$")]
            public string Code { get; set; }
        }
    }
}
