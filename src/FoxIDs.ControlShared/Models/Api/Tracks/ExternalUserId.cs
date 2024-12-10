using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ExternalUserId : IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        public string UpPartyName { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        public string LinkClaimValue { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        public string RedemptionClaimValue { get; set; }

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (LinkClaimValue.IsNullOrWhiteSpace() && RedemptionClaimValue.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(LinkClaimValue)} is required if the field {nameof(RedemptionClaimValue)} is empty.", [nameof(LinkClaimValue), nameof(RedemptionClaimValue)]));
            }
            return results;
        }
    }
}
