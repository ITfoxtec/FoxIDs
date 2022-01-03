namespace FoxIDs.Client.Models.ViewModels
{
    /// <summary>
    /// Added because System.Security.Cryptography.X509Certificates is not supported in the browser.
    /// </summary>
    public enum X509RevocationMode
    {
        NoCheck,
        Online,
        Offline
    }
}
