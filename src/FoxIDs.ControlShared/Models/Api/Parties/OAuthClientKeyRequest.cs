using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// OAuth 2.0 client key request.
    /// </summary>
    public class OAuthClientKeyRequest 
    {
        /// <summary>
        /// OAuth 2.0 or OIDC party name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string PartyName { get; set; }

        /// <summary>
        /// Base64 url encode certificate.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Certificate.EncodeCertificateLength)]
        public string Certificate { get; set; }

        /// <summary>
        /// Certificate password
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        public string Password { get; set; }
    }
}
