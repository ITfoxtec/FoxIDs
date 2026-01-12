using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class ExternalUser : DataDocument, IValidatableObject
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.ExternalUser}:{idKey.TenantName}:{idKey.TrackName}:{idKey.UpPartyName}:{idKey.LinkClaimValueHash}";
        }

        public static async Task<string> IdFormatAsync(RouteBinding routeBinding, string upPartyName, string LinkClaimValueHash)
        {
            if (routeBinding == null) new ArgumentNullException(nameof(routeBinding));
            if (upPartyName == null) new ArgumentNullException(nameof(upPartyName));
            if (LinkClaimValueHash == null) new ArgumentNullException(nameof(LinkClaimValueHash));

            var idKey = new IdKey
            {
                TenantName = routeBinding.TenantName,
                TrackName = routeBinding.TrackName,
                UpPartyName = upPartyName,
                LinkClaimValueHash = LinkClaimValueHash
            };

            return await IdFormatAsync(idKey);
        }

        [Required]
        [MaxLength(Constants.Models.ExternalUser.IdLength)]
        [RegularExpression(Constants.Models.ExternalUser.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MaxLength(Constants.Models.User.UserIdLength)]
        [JsonProperty(PropertyName = "user_id")]
        public string UserId { get; set; }

        [MaxLength(Constants.Models.Party.NameLength)]
        [JsonProperty(PropertyName = "up_party_name")]
        public string UpPartyName { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "link_claim_value")]
        public string LinkClaimValue { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "redemption_claim_value")]
        public string RedemptionClaimValue { get; set; }

        /// <summary>
        /// External user expiration time.
        /// </summary>
        [JsonProperty(PropertyName = "expire_at")]
        public long? ExpireAt { get; set; }

        /// <summary>
        /// 0 to disable expiration.
        /// </summary>
        [JsonProperty(PropertyName = "expire_in_seconds")]
        public int? ExpireInSeconds { get; set; }

        [JsonProperty(PropertyName = "disable_account")]
        public bool DisableAccount { get; set; }

        [ListLength(Constants.Models.User.ClaimsMin, Constants.Models.User.ClaimsMax)]
        [JsonProperty(PropertyName = "claims")]
        public List<ClaimAndValues> Claims { get; set; }

        public class IdKey : Track.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.Party.NameLength)]
            public string UpPartyName { get; set; }

            [Required]
            [MaxLength(Constants.Models.ExternalUser.LinkClaimValueHashLength)]
            public string LinkClaimValueHash { get; set; }
        }

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
