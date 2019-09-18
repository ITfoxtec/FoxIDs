using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OAuthDownResourceScope
    {
        [Required]
        [MaxLength(Constants.Models.OAuthParty.Client.ResourceScopeLength)]
        public string Resource { get; set; }

        [Length(Constants.Models.OAuthParty.Client.ScopesMin, Constants.Models.OAuthParty.Client.ScopesMax, Constants.Models.OAuthParty.ScopesLength)]
        public List<string> Scopes { get; set; }
    }
}
