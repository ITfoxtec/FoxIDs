using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class OAuthDownClient : OAuthDownClient<OAuthDownScope, OAuthDownClaim> { }
    public class OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        [JsonIgnore]
        internal PartyDataElement Parent { private get; set; }

        [JsonIgnore]
        public string ClientId { get => Parent.Name; }

        [Length(0, 50)]
        [JsonProperty(PropertyName = "resource_scopes")]
        public List<OAuthDownResourceScope> ResourceScopes { get; set; }

        [Length(0, 100)]
        [JsonProperty(PropertyName = "scopes")]
        public List<TScope> Scopes { get; set; }

        [Length(0, 500)]
        [JsonProperty(PropertyName = "claims")]
        public List<TClaim> Claims { get; set; }

        [Length(1, 10, 30)]
        [JsonProperty(PropertyName = "response_types")]
        public List<string> ResponseTypes { get; set; }

        [Length(1, 40, 500)]
        [JsonProperty(PropertyName = "redirect_uris")]
        public List<string> RedirectUris { get; set; }

        [Length(0, 10)]
        [JsonProperty(PropertyName = "secrets")]
        public List<OAuthClientSecret> Secrets { get; set; }

        [Range(10, 900)] // 10 seconds to 15 minutes
        [JsonProperty(PropertyName = "authorization_code_lifetime")]
        public int? AuthorizationCodeLifetime { get; set; }

        [Range(300, 86400)] // 5 minutes to 24 hours
        [JsonProperty(PropertyName = "access_token_lifetime")]
        public int AccessTokenLifetime { get; set; }

        [Range(900, 15768000)] // 15 minutes to 6 month
        [JsonProperty(PropertyName = "refresh_token_lifetime")]
        public int? RefreshTokenLifetime { get; set; }

        [Range(900, 31536000)] // 15 minutes to 12 month
        [JsonProperty(PropertyName = "refresh_token_absolute_lifetime")]
        public int? RefreshTokenAbsoluteLifetime { get; set; }

        [JsonProperty(PropertyName = "refresh_token_use_one_time")]
        public bool? RefreshTokenUseOneTime { get; set; }

        [JsonProperty(PropertyName = "refresh_token_lifetime_unlimited")]
        public bool? RefreshTokenLifetimeUnlimited { get; set; }        
    }
}
