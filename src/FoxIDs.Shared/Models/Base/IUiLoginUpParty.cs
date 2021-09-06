namespace FoxIDs.Models
{
    public interface IUiLoginUpParty : IDataDocument
    {
        bool DisableResetPassword { get; set; }

        string Css { get; set; }
    }
}
