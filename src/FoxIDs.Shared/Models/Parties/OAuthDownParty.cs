using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models
{
    public class OAuthDownParty : OAuthDownParty<OAuthDownClient, OAuthDownScope, OAuthDownClaim> { }    
    public class OAuthDownParty<TClient, TScope, TClaim> : DownParty where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        public OAuthDownParty()
        {
            Type = PartyType.OAuth2.ToString();
        }

        private TClient client;
        [ValidateObject]
        [JsonProperty(PropertyName = "client")]
        public TClient Client
        {
            get => client;
            set
            {
                value.Parent = this;
                client = value;
            }            
        }

        private OAuthDownResource resource;
        [ValidateObject]
        [JsonProperty(PropertyName = "resource")]
        public OAuthDownResource Resource
        {
            get => resource;
            set
            {
                value.Parent = this;
                resource = value;
            }
        }
        
        [Length(0, 40, 200)]
        [JsonProperty(PropertyName = "allow_cors_origins")]
        public List<string> AllowCorsOrigins { get; set; }
    }
}
