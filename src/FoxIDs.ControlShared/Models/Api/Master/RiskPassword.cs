using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class RiskPassword
    {
        [Required]
        [MaxLength(Constants.Models.RiskPassword.PasswordSha1HashLength)]
        [RegularExpression(Constants.Models.RiskPassword.PasswordSha1HashRegExPattern)]
        public string PasswordSha1Hash { get; set; }

        [Required]
        [Min(Constants.Models.RiskPassword.CountMin)]
        public long Count { get; set; }
    }
}
