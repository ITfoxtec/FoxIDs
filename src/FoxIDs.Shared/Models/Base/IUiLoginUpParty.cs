namespace FoxIDs.Models
{
    public interface IUiLoginUpParty : IDataDocument
    {
        bool DisableResetPassword { get; set; }

        string CssStyle { get; set; }
    }
}
