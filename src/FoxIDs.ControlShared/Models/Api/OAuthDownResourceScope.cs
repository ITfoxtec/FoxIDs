using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OAuthDownResourceScope
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        public string Resource { get; set; }

        [Length(Constants.Models.OAuthDownParty.Client.ScopesMin, Constants.Models.OAuthDownParty.Client.ScopesMax, Constants.Models.OAuthDownParty.ScopesLength)]
        public List<string> Scopes { get; set; }
    }
}
