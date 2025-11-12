using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class DownPartyTestResultResponse
    {
        /// <summary>
        /// Test application name.
        /// </summary>
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }

        [ListLength(Constants.Models.OAuthDownParty.Client.ClaimsMin, Constants.Models.OAuthDownParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [Display(Name = "ID token claims")]
        public List<ClaimAndValues> IdTokenClaims { get; set; }

        [ListLength(Constants.Models.OAuthDownParty.Client.ClaimsMin, Constants.Models.OAuthDownParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [Display(Name = "Access token claims")]
        public List<ClaimAndValues> AccessTokenClaims { get; set; }

        [Required]
        [MaxLength(IdentityConstants.MessageLength.TokenMax)]
        [Display(Name = "Id token")]
        public string IdToken { get; set; }

        [Required]
        [MaxLength(IdentityConstants.MessageLength.TokenMax)]
        [Display(Name = "Access token")]
        public string AccessToken { get; set; }

        [MaxLength(Constants.Models.DownParty.UrlLengthMax)]
        [Display(Name = "End session URL")]
        public string EndSessionUrl { get; set; }

        [MaxLength(Constants.Models.DownParty.UrlLengthMax)]
        [Display(Name = "Test URL")]
        public string TestUrl { get; set; }

        /// <summary>
        /// Test expiration time in Unix time seconds.
        /// </summary>
        public long TestExpireAt { get; set; }

        /// <summary>
        /// 0 to disable expiration.
        /// </summary>
        public int TestExpireInSeconds { get; set; }
    }
}
