using FoxIDs.Models.Api;
using System;

namespace FoxIDs.Client.Models.ViewModels
{
    public class CertificateInfoViewModel
    {
        public string Subject { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsValid { get; set; }
        public string Thumbprint { get; set; }
        public JsonWebKey Key { get; set; }
    }
}
