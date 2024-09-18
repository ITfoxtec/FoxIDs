using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class DownPartyTestStartRequest
    {
        /// <summary>
        /// Allow authentication methods.
        /// </summary>
        [ListLength(Constants.Models.OidcDownPartyTest.UpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax)]
        public List<UpPartyLink> UpParties { get; set; }

        [ListLength(Constants.Models.OidcDownPartyTest.ClaimsMin, Constants.Models.OAuthDownParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [Display(Name = "Issue claims (use * to issue all claims)")]
        public List<string> Claims { get; set; } = new List<string> { "*" };

        [Required]
        [MaxLength(Constants.Models.OAuthDownParty.Client.RedirectUriLength)]
        [JsonProperty(PropertyName = "Redirect URI")]
        public string RedirectUri { get; set; }

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.Client.ResponseModeLength)]
        [Display(Name = "Response mode")]
        public string ResponseMode { get; set; } = IdentityConstants.ResponseModes.Fragment;
    }
}
