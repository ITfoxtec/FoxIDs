using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class CertificateAndPassword
    {
        /// <summary>
        /// Base64 url encode certificate.
        /// </summary>
        [Required]
        public string EncodeCertificate { get; set; }

        /// <summary>
        /// Optionally password
        /// </summary>
        public string Password { get; set; }
    }
}
