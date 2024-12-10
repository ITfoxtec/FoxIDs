using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.Api
{
    public class ExternalUserUpdateRequest : ExternalUserRequest
    {
        /// <summary>
        /// The <see cref="LinkClaimValue" /> is updated with the value in <see cref="UpdateLinkClaimValue" />. The <see cref="LinkClaimValue" /> become empty is the <see cref="UpdateLinkClaimValue" /> is empty.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        public string UpdateLinkClaimValue { get; set; }

        /// <summary>
        /// The <see cref="RedemptionClaimValue" /> is updated with the value in <see cref="UpdateRedemptionClaimValue" />. The <see cref="RedemptionClaimValue" /> become empty is the <see cref="UpdateRedemptionClaimValue" /> is empty.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        public string UpdateRedemptionClaimValue { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (UpdateLinkClaimValue.IsNullOrWhiteSpace() && UpdateRedemptionClaimValue.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(UpdateLinkClaimValue)} is required if the field {nameof(UpdateRedemptionClaimValue)} is empty.", [nameof(UpdateLinkClaimValue), nameof(UpdateRedemptionClaimValue)]));
            }

            var baseResults = base.Validate(validationContext);
            if (baseResults.Count() > 0)
            {
                results.AddRange(baseResults);
            }
            return results;
        }
    }
}
