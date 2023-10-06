using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class CertificateAndPassword
    {
        /// <summary>
        /// Base64 url encode certificate.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Certificate.EncodeCertificateLength)]
        public string EncodeCertificate { get; set; }

        /// <summary>
        /// Optionally password
        /// </summary>
        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        public string Password { get; set; }
    }
}
