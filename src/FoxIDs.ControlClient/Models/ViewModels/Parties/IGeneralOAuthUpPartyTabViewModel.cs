namespace FoxIDs.Client.Models.ViewModels
{
    public interface IGeneralOAuthUpPartyTabViewModel
    {
        bool ShowClientTab { get; set; }
        bool ShowClaimTransformTab { get; set; }
        public bool ShowSessionTab { get; set; }

        public bool ShowHrdTab { get; set; }
    }
}
