using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OAuthUpClient
    {
        [MaxLength(Constants.Models.OAuthUpParty.Client.ClientIdLength)]
        [Display(Name = "Optional custom SP client ID")]
        public string SpClientId { get; set; }

        [ListLength(Constants.Models.OAuthUpParty.Client.ClaimsMin, Constants.Models.OAuthUpParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [Display(Name = "Forward claims (use * to carried all claims forward)")]
        public List<string> Claims { get; set; }

        [MaxLength(Constants.Models.OAuthUpParty.Client.UserInfoUrlLength)]
        [Display(Name = "UserInfo URL")]
        public string UserInfoUrl { get; set; }

        [Display(Name = "Read claims from the UserInfo Endpoint instead of the access token or ID token")]
        public bool UseUserInfoClaims { get; set; }
    }
}
