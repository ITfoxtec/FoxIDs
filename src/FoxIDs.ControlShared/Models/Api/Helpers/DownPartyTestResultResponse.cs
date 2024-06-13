using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class DownPartyTestResultResponse
    {
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
    }
}
