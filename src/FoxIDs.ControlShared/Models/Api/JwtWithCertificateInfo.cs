using ITfoxtec.Identity.Models;

namespace FoxIDs.Models.Api
{
    public class JwtWithCertificateInfo : JsonWebKey
    {
        public CertificateInfo CertificateInfo { get; set; }
    }
}
