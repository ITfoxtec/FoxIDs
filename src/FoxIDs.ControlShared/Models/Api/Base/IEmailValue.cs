namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Defines an email value used as an identifier in API messages.
    /// </summary>
    public interface IEmailValue
    {
        /// <summary>
        /// User email address.
        /// </summary>
        string Email { get; set; }
    }
}
