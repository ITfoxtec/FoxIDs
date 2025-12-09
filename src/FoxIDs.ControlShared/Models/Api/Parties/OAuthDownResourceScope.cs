using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Maps a resource to the scopes requested by a client.
    /// </summary>
    public class OAuthDownResourceScope
    {
        /// <summary>
        /// Target resource name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [Display(Name = "Resource")]
        public string Resource { get; set; }

        /// <summary>
        /// Scopes requested on the resource.
        /// </summary>
        [ListLength(Constants.Models.OAuthDownParty.Client.ScopesMin, Constants.Models.OAuthDownParty.Client.ScopesMax, Constants.Models.OAuthDownParty.ScopeLength, Constants.Models.OAuthDownParty.ScopeRegExPattern)]
        [Display(Name = "Scopes")]
        public List<string> Scopes { get; set; }
    }
}
