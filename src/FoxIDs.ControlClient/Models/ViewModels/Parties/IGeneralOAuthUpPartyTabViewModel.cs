namespace FoxIDs.Client.Models.ViewModels
{
    public interface IGeneralOAuthUpPartyTabViewModel
    {
        bool ShowClientTab { get; set; }
        bool ShowClaimTransformTab { get; set; }
        public bool ShowExtendedUiTab { get; set; }
        public bool ShowLinkExternalUserTab { get; set; }
        public bool ShowHrdTab { get; set; }
        public bool ShowProfileTab { get; set; }
        public bool ShowSessionTab { get; set; }
    }
}
