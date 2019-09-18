using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class OAuthDownResource
    {
        [Length(Constants.Models.OAuthParty.Resource.ScopesMin, Constants.Models.OAuthParty.Resource.ScopesMax, Constants.Models.OAuthParty.ScopesLength)]
        public List<string> Scopes { get; set; }
    }
}
