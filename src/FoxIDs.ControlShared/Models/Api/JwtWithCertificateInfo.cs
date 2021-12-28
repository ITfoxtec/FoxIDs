using ITfoxtec.Identity.Models;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class JwtWithCertificateInfo : JsonWebKey
    {
        [Required]
        public CertificateInfo CertificateInfo { get; set; }
    }
}
