namespace FoxIDs.Models
{
    public interface IUiLoginUpParty : IDataDocument
    {
        bool DisableResetPassword { get; set; }

        public string Title { get; set; }
        public string IconUrl { get; set; }
        string Css { get; set; }
    }
}
