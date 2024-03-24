using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralExternalUserViewModel : ExternalUser
    {
        public GeneralExternalUserViewModel()
        { }

        public GeneralExternalUserViewModel(ExternalUser externalUser)
        {
            UpPartyName = externalUser.UpPartyName;
            LinkClaimValue = externalUser.LinkClaimValue;
        }

        public string UpPartyDisplayName { get; set; }

        public bool Edit { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool CreateMode { get; set; }

        public bool DeleteAcknowledge { get; set; }

        public string Error { get; set; }

        public PageEditForm<ExternalUserViewModel> Form { get; set; }
    }
}
