using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Identifies an external user link for an upstream party.
    /// </summary>
    public class ExternalUserId : IValidatableObject
    {
        /// <summary>
        /// Up-party name the external user belongs to.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        public string UpPartyName { get; set; }

        /// <summary>
        /// Value used when linking the external account.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        public string LinkClaimValue { get; set; }

        /// <summary>
        /// Value expected when redeeming the link.
        /// </summary>
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
