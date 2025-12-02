using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class PasswordHistoryItem
    {
        [Required]
        [MaxLength(Constants.Models.SecretHash.HashAlgorithmLength)]
        [JsonProperty(PropertyName = "hash_algorithm")]
        public string HashAlgorithm { get; set; }

        [Required]
        [MaxLength(Constants.Models.SecretHash.PasswordHistoryHashLength)]
        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }

        [JsonProperty(PropertyName = "created")]
        public long Created { get; set; }
    }
}
