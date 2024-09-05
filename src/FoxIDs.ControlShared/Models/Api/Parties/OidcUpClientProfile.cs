using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class OidcUpClientProfile
    {
        /// <summary>
        /// Add additional scopes.
        /// </summary>
        [ListLength(Constants.Models.OAuthUpParty.Client.ScopesMin, Constants.Models.OAuthUpParty.Client.ScopesMax, Constants.Models.OAuthUpParty.ScopeLength, Constants.Models.OAuthUpParty.ScopeRegExPattern)]
        public List<string> Scopes { get; set; }

        /// <summary>
        /// Add additional parameter or change parameter values.
        /// </summary>
        [ListLength(Constants.Models.OAuthUpParty.Client.AdditionalParametersMin, Constants.Models.OAuthUpParty.Client.AdditionalParametersMax)]
        public List<OAuthAdditionalParameter> AdditionalParameters { get; set; }
    }
}
