using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class RefreshTokenGrant
    {
        [MaxLength(Constants.Models.Party.NameLength)]
        public string ClientId { get; set; }

        [MaxLength(IdentityConstants.MessageLength.SessionIdMax)]
        public string SessionId { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        public string Sub { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        public string Email { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        public string Phone { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        public string Username { get; set; }

        public DateTime? ExpireAt { get; set; }

        [ListLength(Constants.Models.OAuthDownParty.Grant.ClaimsMin, Constants.Models.OAuthDownParty.Grant.ClaimsMax)]
        public List<ClaimAndValues> Claims { get; set; }
    }
}
