using ITfoxtec.Identity.Models;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// JSON Web Key enriched with certificate metadata for swagger display.
    /// </summary>
    public class JwkWithCertificateInfo : JsonWebKey
    {
        /// <summary>
        /// Certificate details bound to the key, if available.
        /// </summary>
        public CertificateInfo CertificateInfo { get; set; }
    }
}
