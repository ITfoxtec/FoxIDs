using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models
{
    public class OAuthDownResource
    {
        [JsonIgnore]
        internal PartyDataElement Parent { private get; set; }

        [JsonIgnore]
        public string ResourceId { get => Parent.Name; }

        [Length(Constants.Models.OAuthDownParty.Resource.ScopesMin, Constants.Models.OAuthDownParty.Resource.ScopesMax, Constants.Models.OAuthDownParty.ScopesLength)]
        [JsonProperty(PropertyName = "scopes")]
        public List<string> Scopes { get; set; }
    }
}
