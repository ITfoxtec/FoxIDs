using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralSamlUpPartyViewModel : GeneralUpPartyViewModel
    {
        public const string DefaultCertificateFileStatus = "Drop certificate files here or click to select";
        public const int CertificateMaxFileSize = 5 * 1024 * 1024; // 5MB

        public UpPartyModuleTypes? ModuleType { get; set; }

        public GeneralSamlUpPartyViewModel() : base(PartyTypes.Saml2)
        { }

        public GeneralSamlUpPartyViewModel(UpParty upParty) : base(upParty)
        { }

        public PageEditForm<SamlUpPartyViewModel> Form { get; set; }

        public List<KeyInfoViewModel> KeyInfoList { get; set; } = new List<KeyInfoViewModel>();

        public string CertificateFileStatus { get; set; } = DefaultCertificateFileStatus;

        public bool ShowStandardSettings { get; set; }

        public bool ShowSamlTab { get; set; } = true;
        public bool ShowClaimTransformTab { get; set; }
        public bool ShowExtendedUiTab { get; set; }
        public bool ShowLinkExternalUserTab { get; set; }
        public bool ShowHrdTab { get; set; }
        public bool ShowProfileTab { get; set; }
        public bool ShowSessionTab { get; set; }

        public bool ShowAuthorityDetails { get; set; }
    }
}
