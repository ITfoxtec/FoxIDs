using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralOidcDownPartyViewModel : GeneralDownPartyViewModel, IGeneralOAuthDownPartyTabViewModel
    {
        public const string DefaultClientCertificateFileStatus = "Drop client certificate files here or click to select";

        public GeneralOidcDownPartyViewModel() : base(PartyTypes.Oidc)
        { }

        public GeneralOidcDownPartyViewModel(DownParty downParty) : base(downParty)
        { }

        public PageEditForm<OidcDownPartyViewModel> Form { get; set; }

        public SelectUpParty<OidcDownPartyViewModel> SelectAllowUpPartyName;

        public List<KeyInfoViewModel> ClientKeyInfoList { get; set; } = new List<KeyInfoViewModel>();

        public string ClientCertificateFileStatus { get; set; } = DefaultClientCertificateFileStatus;

        [Display(Name = "Down-party type")]
        public DownPartyOAuthTypes DownPartyType { get; set; } = DownPartyOAuthTypes.Client;

        public bool ShowClientTab { get; set; } = true;
        public bool ShowResourceTab { get; set; }
        public bool ShowClaimTransformTab { get; set; }
    }
}
