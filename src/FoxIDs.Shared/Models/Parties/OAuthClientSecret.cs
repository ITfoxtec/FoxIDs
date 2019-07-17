using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class OAuthClientSecret : ISecretHash
    {
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
    }
}
