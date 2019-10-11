using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    /// <summary>
    /// OAuth 2.0 down party.
    /// </summary>
    public class OAuthDownParty : OAuthDownParty<OAuthDownClient, OAuthDownScope, OAuthDownClaim> { }
    /// <summary>
    /// OAuth 2.0 down party.
    /// </summary>
    public class OAuthDownParty<TClient, TScope, TClaim> : DownParty, IValidatableObject where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        public OAuthDownParty()
        {
            Type = PartyType.OAuth2;
        }

        private TClient client;
        /// <summary>
        /// OAuth 2.0 down client.
        /// </summary>
        [ValidateObject]
        [JsonProperty(PropertyName = "client")]
        public TClient Client
        {
            get => client;
            set
            {
                if(value != null) value.Parent = this;
                client = value;
            }            
        }

        private OAuthDownResource resource;
        /// <summary>
        /// OAuth 2.0 down resource.
        /// </summary>
        [ValidateObject]
        [JsonProperty(PropertyName = "resource")]
        public OAuthDownResource Resource
        {
            get => resource;
            set
            {
                if (value != null) value.Parent = this;
                resource = value;
            }
        }

        /// <summary>
        /// Allow cors origins.
        /// </summary>
        [Length(Constants.Models.OAuthDownParty.AllowCorsOriginsMin, Constants.Models.OAuthDownParty.AllowCorsOriginsMax, Constants.Models.OAuthDownParty.AllowCorsOriginLength)]
        [JsonProperty(PropertyName = "allow_cors_origins")]
        public List<string> AllowCorsOrigins { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Client == null && Resource == null)
            {
                results.Add(new ValidationResult($"Either the field {nameof(Client)} or the field {nameof(Resource)} is required.", new[] { nameof(Client), nameof(Resource) }));
            }
            return results;
        }
    }
}
