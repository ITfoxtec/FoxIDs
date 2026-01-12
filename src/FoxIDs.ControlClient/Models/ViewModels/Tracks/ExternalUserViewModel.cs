using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class ExternalUserViewModel : IValidatableObject
    {
        public ExternalUserViewModel()
        {
            Claims = new List<ClaimAndValues>();
        }

        [Required(ErrorMessage = "Select which authentication method the external user must be assigned to.")]
        [MaxLength(Constants.Models.Party.NameLength)]
        [Display(Name = "Connected authentication method")]
        public string UpPartyName { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [Display(Name = "Connected authentication method")]
        public string UpPartyDisplayName { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Link claim value")]
        public string LinkClaimValue { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Redemption claim value")]
        public string RedemptionClaimValue { get; set; }

        [MaxLength(Constants.Models.User.UserIdLength)]
        [Display(Name = "User id (unique and persistent)")]
        public string UserId { get; set; }

        public long? ExpireAt { get; set; }

        [Display(Name = "Expire at")]
        public string ExpireAtText
        {
            get
            {
                return ExpireAt.HasValue && ExpireAt.Value > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(ExpireAt.Value).ToUniversalTime().ToLocalTime().ToString()
                    : string.Empty;
            }
            set { }
        }

        [Display(Name = "Account status")]
        public bool DisableAccount { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.User.ClaimsMin, Constants.Models.User.ClaimsMax)]
        [Display(Name = "Claims")]
        public List<ClaimAndValues> Claims { get; set; }

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (LinkClaimValue.IsNullOrWhiteSpace() && RedemptionClaimValue.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"A unique link claim value or redemption claim value is required.", [nameof(LinkClaimValue), nameof(RedemptionClaimValue)]));
            }
            return results;
        }
    }
}
