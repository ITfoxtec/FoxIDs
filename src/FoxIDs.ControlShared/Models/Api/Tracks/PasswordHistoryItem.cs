using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class PasswordHistoryItem
    {
        [MaxLength(Constants.Models.SecretHash.HashAlgorithmLength)]
        public string HashAlgorithm { get; set; }

        [MaxLength(Constants.Models.SecretHash.HashLength)]
        public string Hash { get; set; }

        public long Created { get; set; }
    }
}