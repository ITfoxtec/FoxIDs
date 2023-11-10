using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models
{
    public class ClaimAndValues : IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [JsonProperty(PropertyName = "claim")]
        public string Claim { get; set; }

        [Length(Constants.Models.Claim.ValuesUserMin, Constants.Models.Claim.ValuesMax)]
        [JsonProperty(PropertyName = "values")]
        public List<string> Values { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Claim.Length > Constants.Models.Claim.ProcessValueLength)
            {
                results.Add(new ValidationResult($"Claim '{Claim}' value is too long, maximum length of '{Constants.Models.Claim.ProcessValueLength}'."));
            }
            return results;
        }
    }
}
