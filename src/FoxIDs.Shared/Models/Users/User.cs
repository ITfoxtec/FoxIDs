using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class User : DataDocument, ISecretHash
    {
        public static async Task<string> IdFormat(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"user:{idKey.TenantName}:{idKey.TrackName}:{idKey.Email}";
        }

        [Required]
        [MaxLength(140)]
        [RegularExpression(@"^[\w:\-.+@]*$")]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MaxLength(40)]
        [JsonProperty(PropertyName = "user_id")]
        public string UserId { get; set; }

        [Required]
        [MaxLength(20)]
        [JsonProperty(PropertyName = "hash_algorithm")]
        public string HashAlgorithm { get; set; }

        [Required]
        [MaxLength(2048)]
        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }

        [Required]
        [MaxLength(512)]
        [JsonProperty(PropertyName = "hash_salt")]
        public string HashSalt { get; set; }

        [JsonIgnore]
        public string Email => Id.Substring(Id.LastIndexOf(':') + 1); 

        [Length(0, 100)]
        [JsonProperty(PropertyName = "claims")]
        public List<ClaimAndValues> Claims { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormat(idKey);
        }

        public class IdKey : Track.IdKey
        {
            [Required]
            [MaxLength(60)]
            [RegularExpression(@"^[\w:\-.+@]*$")]
            public string Email { get; set; }
        }
    }
}
