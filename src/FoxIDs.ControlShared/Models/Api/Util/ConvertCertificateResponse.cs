using ITfoxtec.Identity.Models;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ConvertCertificateResponse
    {
        /// <summary>
        /// Certificate as JsonWebKey.
        /// </summary>
        [Required]
        public JsonWebKey Key { get; set; }
    }
}
