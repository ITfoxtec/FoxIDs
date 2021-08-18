using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ConvertCertificateResponse
    {
        /// <summary>
        /// Certificate as base64 encoded byres.
        /// </summary>
        [Required]
        public string Bytes { get; set; }
    }
}
