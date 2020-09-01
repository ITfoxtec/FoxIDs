using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using MTokens = Microsoft.IdentityModel.Tokens;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralTrackCertificateViewModel : TrackCertificateInfoViewModel
    {
        public const string DefaultCertificateFileStatus = "Drop certificate file here or click to select";
        public const int CertificateMaxFileSize = 5 * 1024 * 1024; // 5MB

        public GeneralTrackCertificateViewModel(bool isPrimary)
        {
            IsPrimary = isPrimary;
        }

        public GeneralTrackCertificateViewModel(TrackKey trackKey, bool isPrimary) : this(isPrimary)
        {
            var certificate = new MTokens.JsonWebKey(trackKey.Key.JsonSerialize()).ToX509Certificate();
            Subject = certificate.Subject;
            ValidFrom = certificate.NotBefore;
            ValidTo = certificate.NotAfter;
            IsValid = certificate.IsValid();
            Thumbprint = certificate.Thumbprint;
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
