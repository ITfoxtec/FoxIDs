using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Public key material used for OAuth/OIDC client authentication.
    /// </summary>
    public class ClientKey
    {
        /// <summary>
        /// Type of client key storage.
        /// </summary>
        [Required]
        public ClientKeyTypes Type { get; set; }

        /// <summary>
        /// External name of the key in the identity provider.
        /// </summary>
        [Required]
        public string ExternalName { get; set; }

        /// <summary>
        /// Public key used to validate client assertions.
        /// </summary>
        [Required]
        public JwkWithCertificateInfo PublicKey { get; set; }
    }
}
