using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class OAuthDownResourceScope
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [JsonProperty(PropertyName = "resource")]
        public string Resource { get; set; }

        [Length(Constants.Models.OAuthDownParty.Client.ScopesMin, Constants.Models.OAuthDownParty.Client.ScopesMax, Constants.Models.OAuthDownParty.ScopesLength)]
        [JsonProperty(PropertyName = "scopes")]
        public List<string> Scopes { get; set; }
    }
}
