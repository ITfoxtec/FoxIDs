using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;


namespace FoxIDs.Models
{
    public class OidcDownPartyTest : OidcDownParty<OidcDownClient, OidcDownScope, OidcDownClaim>
    {
        [MaxLength(IdentityConstants.MessageLength.CodeVerifierMax)]
        [JsonProperty(PropertyName = "code_verifier")]
        public string CodeVerifier { get; set; }

        [MaxLength(IdentityConstants.MessageLength.NonceMax)]
        [JsonProperty(PropertyName = "nonce")]
        public string Nonce { get; set; }
    }
}
