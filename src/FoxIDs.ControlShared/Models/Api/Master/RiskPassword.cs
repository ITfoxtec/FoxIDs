using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Known compromised password hash entry with occurrence count.
    /// </summary>
    public class RiskPassword
    {
        /// <summary>
        /// SHA1 hash of the risky password.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.RiskPassword.PasswordSha1HashLength)]
        [RegularExpression(Constants.Models.RiskPassword.PasswordSha1HashRegExPattern)]
        public string PasswordSha1Hash { get; set; }

        /// <summary>
        /// Number of breaches the password appeared in.
        /// </summary>
        [Required]
        [Min(Constants.Models.RiskPassword.CountMin)]
        public long Count { get; set; }
    }
}
