using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class OAuthDownResource
    {
        [Length(Constants.Models.OAuthDownParty.Resource.ScopesMin, Constants.Models.OAuthDownParty.Resource.ScopesMax, Constants.Models.OAuthDownParty.ScopesLength)]
        public List<string> Scopes { get; set; }
    }
}
