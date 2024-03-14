using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class ExternalUser : DataDocument
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.ExternalUser}:{idKey.TenantName}:{idKey.TrackName}:{idKey.UpPartyName}:{idKey.LinkClaimHash}";
        }

        public static async Task<string> IdFormatAsync(RouteBinding routeBinding, string upPartyName, string LinkClaimHash)
        {
            if (routeBinding == null) new ArgumentNullException(nameof(routeBinding));
            if (upPartyName == null) new ArgumentNullException(nameof(upPartyName));
            if (LinkClaimHash == null) new ArgumentNullException(nameof(LinkClaimHash));

            var idKey = new IdKey
            {
                TenantName = routeBinding.TenantName,
                TrackName = routeBinding.TrackName,
                UpPartyName = upPartyName,
                LinkClaimHash = LinkClaimHash
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

        [Required]
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "link_claim")]
        public string LinkClaim { get; set; }

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
            [MaxLength(Constants.Models.ExternalUser.LinkClaimHashLength)]
            public string LinkClaimHash { get; set; }
        }
    }
}
