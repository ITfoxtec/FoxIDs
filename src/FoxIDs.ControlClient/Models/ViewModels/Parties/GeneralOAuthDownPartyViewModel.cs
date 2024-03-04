using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralOAuthDownPartyViewModel : GeneralDownPartyViewModel, IGeneralOAuthDownPartyTabViewModel
    {
        public const string DefaultClientCertificateFileStatus = "Drop client certificate files here or click to select";

        public GeneralOAuthDownPartyViewModel() : base(PartyTypes.OAuth2)
        { }

        public GeneralOAuthDownPartyViewModel(DownParty downParty) : base(downParty)
        { }

        public PageEditForm<OAuthDownPartyViewModel> Form { get; set; }

        public SelectUpParty<OAuthDownPartyViewModel> SelectAllowUpPartyName;

        public List<KeyInfoViewModel> ClientKeyInfoList { get; set; } = new List<KeyInfoViewModel>();

        public string ClientCertificateFileStatus { get; set; } = DefaultClientCertificateFileStatus;

        [Display(Name = "Application registration type")]
        public DownPartyOAuthTypes DownPartyType { get; set; } = DownPartyOAuthTypes.Resource;

        public bool ShowClientTab { get; set; }
        public bool ShowResourceTab { get; set; } = true;
        public bool ShowClaimTransformTab { get; set; }
    }
}
