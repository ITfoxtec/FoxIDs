using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class RiskPasswordItem
    {
        [Required]
        [MaxLength(40)]
        [RegularExpression(@"^[A-F0-9]*$")]
        public string PasswordSha1Hash { get; set; }

        [Required]
        [Min(1)]
        public long Count { get; set; }
    }
}
