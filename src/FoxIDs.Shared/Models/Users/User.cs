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

        public static async Task<string> IdFormat(RouteBinding routeBinding, string email)
        {
            if (routeBinding == null) new ArgumentNullException(nameof(routeBinding));
            if (email == null) new ArgumentNullException(nameof(email));

            var idKey = new IdKey
            {
                TenantName = routeBinding.TenantName,
                TrackName = routeBinding.TrackName,
                Email = email
            };

            return await IdFormat(idKey);
        }

        [Required]
        [MaxLength(Constants.Models.User.IdLength)]
        [RegularExpression(Constants.Models.User.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MaxLength(Constants.Models.User.UserIdLength)]
        [JsonProperty(PropertyName = "user_id")]
        public string UserId { get; set; }        

        [Required]
        [MaxLength(Constants.Models.SecretHash.HashAlgorithmLength)]
        [JsonProperty(PropertyName = "hash_algorithm")]
        public string HashAlgorithm { get; set; }

        [Required]
        [MaxLength(Constants.Models.SecretHash.HashLength)]
        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }

        [Required]
        [MaxLength(Constants.Models.SecretHash.HashSaltLength)]
        [JsonProperty(PropertyName = "hash_salt")]
        public string HashSalt { get; set; }

        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        [Length(Constants.Models.User.ClaimsMin, Constants.Models.User.ClaimsMax)]
        [JsonProperty(PropertyName = "claims")]
        public List<ClaimAndValues> Claims { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormat(idKey);
            Email = Id.Substring(Id.LastIndexOf(':') + 1); 
        }

        public class IdKey : Track.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.User.EmailLength)]
            [RegularExpression(Constants.Models.User.EmailRegExPattern)]
            public string Email { get; set; }
        }
    }
}
