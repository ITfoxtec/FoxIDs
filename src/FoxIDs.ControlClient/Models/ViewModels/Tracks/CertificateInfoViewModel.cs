using ITfoxtec.Identity.Models;
using System;
using System.ComponentModel.DataAnnotations;

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

        [Display(Name = "Optional certificate password")]
        public string Password { get; set; }        
    }
}
