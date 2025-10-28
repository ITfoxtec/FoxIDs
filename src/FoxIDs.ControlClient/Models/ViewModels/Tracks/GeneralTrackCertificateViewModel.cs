using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System.Linq;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralTrackCertificateViewModel : TrackCertificateInfoViewModel
    {
        public const string DefaultCertificateFileStatus = "Drop .PFX or .P12 certificate file here or click to select";
        public const int CertificateMaxFileSize = 5 * 1024 * 1024; // 5MB

        public GeneralTrackCertificateViewModel(bool isPrimary)
        {
            IsPrimary = isPrimary;
        }

        public GeneralTrackCertificateViewModel(JwkWithCertificateInfo key, bool isPrimary) : this(isPrimary)
        {
            Subject = key.CertificateInfo.Subject;
            ValidFrom = key.CertificateInfo.ValidFrom;
            ValidTo = key.CertificateInfo.ValidTo;
            IsValid = key.CertificateInfo.IsValid();
            Thumbprint = key.CertificateInfo.Thumbprint;
            Key = key;
            KeyId = key.Kid;
            CertificateBase64 = key.X5c?.FirstOrDefault();
        }

        public bool Edit { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool CreateMode { get; set; }

        public bool DeleteAcknowledge { get; set; }

        public string Error { get; set; }

        public PageEditForm<TrackCertificateInfoViewModel> Form { get; set; }

        public string CertificateFileStatus { get; set; } = DefaultCertificateFileStatus;
    }
}