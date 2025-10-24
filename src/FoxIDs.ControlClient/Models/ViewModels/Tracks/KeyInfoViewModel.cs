using FoxIDs.Models.Api;
using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class KeyInfoViewModel
    {
        public string Subject { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsValid { get; set; }
        public string Thumbprint { get; set; }
        public string KeyId { get; set; }
        public JwkWithCertificateInfo Key { get; set; }

        [Display(Name = "Optional certificate password")]
        public string Password { get; set; }

        public string Name { get; set; }

        [Display(Name = "Certificate (Base64)")]
        public string CertificateBase64 { get; set; }
    }
}
