using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ConvertCertificateRequest
    {
        /// <summary>
        /// Certificate as base64 encoded byres.
        /// </summary>
        [Required]
        public string Bytes { get; set; }

        /// <summary>
        /// Optional password
        /// </summary>
        public string Password { get; set; }
    }
}
