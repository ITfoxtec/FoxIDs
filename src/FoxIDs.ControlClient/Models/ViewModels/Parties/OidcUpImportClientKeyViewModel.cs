using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OidcUpImportClientKeyViewModel
    {
        public string ClientKeyFileStatus { get; set; } = GeneralTrackCertificateViewModel.DefaultCertificateFileStatus;

        [Display(Name = "Optional certificate password")]
        public string Password { get; set; }

        [Display(Name = "Client certificate")]
        public KeyInfoViewModel PublicClientKeyInfo { get; set; }
    }
}
