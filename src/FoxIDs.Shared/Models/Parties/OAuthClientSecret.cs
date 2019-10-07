using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class OAuthClientSecret : ISecretHash
    {
        [Required]
        [MaxLength(Constants.Models.SecretHash.IdLength)]
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [MaxLength(Constants.Models.SecretHash.InfoLength)]
        [JsonProperty(PropertyName = "info")]
        public string Info { get; set; }

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
    }
}
