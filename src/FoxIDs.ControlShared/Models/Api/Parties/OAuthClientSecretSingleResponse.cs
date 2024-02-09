using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// OAuth 2.0 client secret response - one secret.
    /// </summary>
    public class OAuthClientSecretSingleResponse
    {
        /// <summary>
        /// Secret info.
        /// </summary>
        [MaxLength(Constants.Models.SecretHash.InfoLength)]
        public string Info { get; set; }
    }
}
