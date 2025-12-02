using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class PasswordHistoryItem
    {
        [MaxLength(Constants.Models.SecretHash.HashAlgorithmLength)]
        [JsonProperty(PropertyName = "hash_algorithm")]
        public string HashAlgorithm { get; set; }

        [MaxLength(Constants.Models.SecretHash.PasswordHistoryHashLength)]
        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }

        [JsonProperty(PropertyName = "created")]
        public long Created { get; set; }
    }
}
