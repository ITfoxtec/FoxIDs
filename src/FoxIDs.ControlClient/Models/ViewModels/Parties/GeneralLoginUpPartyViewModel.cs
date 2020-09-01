using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralLoginUpPartyViewModel : GeneralUpPartyViewModel
    {
        public const string DefaultCertificateFileStatus = "Drop certificate files here or click to select";
        public const int CertificateMaxFileSize = 5 * 1024 * 1024; // 5MB

        public GeneralLoginUpPartyViewModel() : base(PartyTypes.Login)
        { }

        public GeneralLoginUpPartyViewModel(UpParty upParty) : base(upParty)
        { }

        public PageEditForm<LoginUpPartyViewModel> Form { get; set; }
    }
}
