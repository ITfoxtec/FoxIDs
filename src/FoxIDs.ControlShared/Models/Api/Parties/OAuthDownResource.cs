using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Resource definition listing supported scopes.
    /// </summary>
    public class OAuthDownResource
    {
        /// <summary>
        /// Scopes associated with the resource.
        /// </summary>
        [ListLength(Constants.Models.OAuthDownParty.Resource.ScopesMin, Constants.Models.OAuthDownParty.Resource.ScopesMax, Constants.Models.OAuthDownParty.ScopeLength, Constants.Models.OAuthDownParty.ScopeRegExPattern)]
        [Display(Name = "Scopes")]
        public List<string> Scopes { get; set; }
    }
}
