namespace FoxIDs.Models.Api
{
    /// <summary>
    /// OAuth 2.0 client key response.
    /// </summary>
    public class OAuthClientKeyResponse 
    {
        /// <summary>
        /// Base64 url encode primary public certificate.
        /// </summary>
        public string PublicCertificate { get; set; }
    }
}
