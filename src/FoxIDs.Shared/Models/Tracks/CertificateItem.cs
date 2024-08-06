using System.Security.Cryptography.X509Certificates;

namespace FoxIDs.Models
{
    public class CertificateItem
    {
        public X509Certificate2 Certificate { get; set; }

        public long NotBefore { get; set; }

        public long NotAfter { get; set; }
    }
}
