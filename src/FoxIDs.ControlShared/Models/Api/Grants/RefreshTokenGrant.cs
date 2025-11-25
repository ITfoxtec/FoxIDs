using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class RefreshTokenGrant
    {
        [MaxLength(Constants.Models.OAuthDownParty.Grant.RefreshTokenLength)]
        [RegularExpression(Constants.Models.OAuthDownParty.Grant.RefreshTokenRegExPattern)]
        public string RefreshToken { get; set; }

        [MaxLength(Constants.Models.Party.NameLength)]
        [Display(Name = "Application (technical name / client ID)")]
        public string ClientId { get; set; }

        [MaxLength(IdentityConstants.MessageLength.SessionIdMax)]
        [Display(Name = "Session ID")]
        public string SessionId { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "SUB claim")]
        public string Sub { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Authentication method (technical name)")]
        public string UpPartyName { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Authentication method type")]
        public string UpPartyType { get; set; }

        [Required]
        [Display(Name = "Create time")]
        public long CreateTime { get; set; }

        [Display(Name = "Time to live")]
        public int? TimeToLive { get; set; }

        [ListLength(Constants.Models.OAuthDownParty.Grant.ClaimsMin, Constants.Models.OAuthDownParty.Grant.ClaimsMax)]
        public List<ClaimAndValues> Claims { get; set; }
    }
}
