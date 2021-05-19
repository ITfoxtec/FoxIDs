using ITfoxtec.Identity.Saml2;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace FoxIDs.Models
{
    public class FoxIDsSaml2Configuration : Saml2Configuration
    {
        public List<X509Certificate2> InvalidSignatureValidationCertificates { get; protected set; } = new List<X509Certificate2>();
    }
}
