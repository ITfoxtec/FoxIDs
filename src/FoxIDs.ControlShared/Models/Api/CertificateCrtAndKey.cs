using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class CertificateCrtAndKey
    {
        /// <summary>
        /// PEM encoded certificate (.crt)
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Certificate.EncodeCertificateLength)]
        public string CertificatePemCrt { get; set; }

        /// <summary>
        /// PEM encoded private key (.key)
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Certificate.EncodeCertificateLength)]
        public string CertificatePemKey { get; set; }
    }
}
