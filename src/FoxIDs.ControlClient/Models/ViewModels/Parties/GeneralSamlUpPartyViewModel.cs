using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralSamlUpPartyViewModel : GeneralUpPartyViewModel
    {
        public const string DefaultCertificateFileStatus = "Drop certificate files here or click to select";
        public const int CertificateMaxFileSize = 5 * 1024 * 1024; // 5MB

        public GeneralSamlUpPartyViewModel() : base(PartyTypes.Saml2)
        { }

        public GeneralSamlUpPartyViewModel(UpParty upParty) : base(upParty)
        { }

        public PageEditForm<SamlUpPartyViewModel> Form { get; set; }

        public List<CertificateInfoViewModel> CertificateInfoList { get; set; } = new List<CertificateInfoViewModel>();

        public string CertificateFileStatus { get; set; } = DefaultCertificateFileStatus;
    }
}
