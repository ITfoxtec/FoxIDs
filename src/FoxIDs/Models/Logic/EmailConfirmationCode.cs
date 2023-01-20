using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class EmailConfirmationCode: ISecretHash
    {
        [Required]
        [MaxLength(Constants.Models.SecretHash.HashAlgorithmLength)]
        public string HashAlgorithm { get; set; }

        [Required]
        [MaxLength(Constants.Models.SecretHash.HashLength)]
        public string Hash { get; set; }

        [Required]
        [MaxLength(Constants.Models.SecretHash.HashSaltLength)]
        public string HashSalt { get; set; }
    }
}
