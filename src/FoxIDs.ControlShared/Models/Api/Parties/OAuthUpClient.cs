using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Configuration for OAuth upstream client interactions.
    /// </summary>
    public class OAuthUpClient
    {
        /// <summary>
        /// Optional client ID override for the upstream provider.
        /// </summary>
        [MaxLength(Constants.Models.OAuthUpParty.Client.ClientIdLength)]
        [Display(Name = "Optional custom SP client ID")]
        public string SpClientId { get; set; }

        /// <summary>
        /// Claims to forward to the upstream provider.
        /// </summary>
        [ListLength(Constants.Models.OAuthUpParty.Client.ClaimsMin, Constants.Models.OAuthUpParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [Display(Name = "Forward claims (use * to carried all claims forward)")]
        public List<string> Claims { get; set; }

        /// <summary>
        /// UserInfo endpoint URL.
        /// </summary>
        [MaxLength(Constants.Models.OAuthUpParty.Client.UserInfoUrlLength)]
        [Display(Name = "UserInfo URL")]
        public string UserInfoUrl { get; set; }

        /// <summary>
        /// Read claims from the UserInfo endpoint instead of tokens.
        /// </summary>
        [Display(Name = "Read claims from the UserInfo Endpoint instead of the access token or ID token")]
        public bool UseUserInfoClaims { get; set; }
    }
}
