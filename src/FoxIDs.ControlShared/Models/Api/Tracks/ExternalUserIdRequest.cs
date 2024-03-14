using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ExternalUserIdRequest : IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        public string UpPartyName { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        public string LinkClaim { get; set; }

        [MaxLength(Constants.Models.ExternalUser.LinkClaimHashLength)]
        public string LinkClaimHash { get; set; }

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (LinkClaim.IsNullOrWhiteSpace() && LinkClaimHash.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"Either the {nameof(LinkClaim)} or the {nameof(LinkClaimHash)} field is required.", new[] { nameof(LinkClaim), nameof(LinkClaimHash) }));
            }
            return results;
        }
    }
}
