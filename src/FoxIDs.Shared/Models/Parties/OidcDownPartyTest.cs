using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;


namespace FoxIDs.Models
{
    public class OidcDownPartyTest : OidcDownParty<OidcDownClient, OidcDownScope, OidcDownClaim>, IDataTtlDocument
    {
        [MaxLength(IdentityConstants.MessageLength.CodeVerifierMax)]
        [JsonProperty(PropertyName = "code_verifier")]
        public string CodeVerifier { get; set; }

        [MaxLength(IdentityConstants.MessageLength.NonceMax)]
        [JsonProperty(PropertyName = "nonce")]
        public string Nonce { get; set; }

        [Required]
        [JsonProperty(PropertyName = "ttl")]
        public int TimeToLive { get; set; }

        [JsonProperty(PropertyName = "expire_at")]
        public DateTime ExpireAt { get { return DateTime.UtcNow.AddSeconds(TimeToLive); } set { } }
    }
}
