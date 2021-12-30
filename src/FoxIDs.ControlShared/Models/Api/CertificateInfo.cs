using System;

namespace FoxIDs.Models.Api
{
    public class CertificateInfo
    {
        public string Subject { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public string Thumbprint { get; set; }
    }
}
