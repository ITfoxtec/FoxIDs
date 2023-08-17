namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Oidc client key response.
    /// </summary>
    public class OidcClientKeyResponse 
    {
        /// <summary>
        /// Base64 url encode primary public certificate.
        /// </summary>
        public string PublicCertificate { get; set; }
    }
}
