using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralSamlDownPartyViewModel : GeneralDownPartyViewModel
    {
        public const string DefaultCertificateFileStatus = "Drop certificate files here or click to select";
        public const int CertificateMaxFileSize = 5 * 1024 * 1024; // 5MB

        public GeneralSamlDownPartyViewModel() : base(PartyTypes.Saml2)
        { }

        public GeneralSamlDownPartyViewModel(DownParty downParty) : base(downParty)
        { }

        public PageEditForm<SamlDownPartyViewModel> Form { get; set; }

        public SelectUpParty<SamlDownPartyViewModel> SelectAllowUpPartyName { get; set; }

        public List<CertificateInfoViewModel> CertificateInfoList { get; set; } = new List<CertificateInfoViewModel>();

        public string CertificateFileStatus { get; set; } = DefaultCertificateFileStatus;
    }
}
