using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Represents a refresh token grant stored for a client session.
    /// </summary>
    public class RefreshTokenGrant
    {
        /// <summary>
        /// Persisted refresh token value.
        /// </summary>
        [MaxLength(Constants.Models.OAuthDownParty.Grant.RefreshTokenLength)]
        [RegularExpression(Constants.Models.OAuthDownParty.Grant.RefreshTokenRegExPattern)]
        public string RefreshToken { get; set; }

        /// <summary>
        /// OAuth client identifier (technical name / client ID).
        /// </summary>
        [MaxLength(Constants.Models.Party.NameLength)]
        [Display(Name = "Application (technical name / client ID)")]
        public string ClientId { get; set; }

        /// <summary>
        /// Associated session identifier.
        /// </summary>
        [MaxLength(IdentityConstants.MessageLength.SessionIdMax)]
        [Display(Name = "Session ID")]
        public string SessionId { get; set; }

        /// <summary>
        /// Subject claim value.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "SUB claim")]
        public string Sub { get; set; }

        /// <summary>
        /// Email claim value.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        /// <summary>
        /// Phone claim value.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        /// <summary>
        /// Username claim value.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Username")]
        public string Username { get; set; }

        /// <summary>
        /// Authentication method used to issue the token.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Authentication method (technical name)")]
        public string UpPartyName { get; set; }

        /// <summary>
        /// Type of the upstream authentication party.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Authentication method type")]
        public string UpPartyType { get; set; }

        /// <summary>
        /// Creation time as Unix epoch seconds.
        /// </summary>
        [Required]
        [Display(Name = "Create time")]
        public long CreateTime { get; set; }

        /// <summary>
        /// Time-to-live in seconds, if set.
        /// </summary>
        [Display(Name = "Time to live")]
        public int? TimeToLive { get; set; }

        /// <summary>
        /// Claims issued in the token.
        /// </summary>
        [ListLength(Constants.Models.OAuthDownParty.Grant.ClaimsMin, Constants.Models.OAuthDownParty.Grant.ClaimsMax)]
        public List<ClaimAndValues> Claims { get; set; }
    }
}
