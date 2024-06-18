using ITfoxtec.Identity.Models;

namespace FoxIDs.Models.Api
{
    public class JwkWithCertificateInfo : JsonWebKey
    {
        public CertificateInfo CertificateInfo { get; set; }
    }
}
