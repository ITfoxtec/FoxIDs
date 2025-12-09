namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Extends a name-based message with a new desired name value.
    /// </summary>
    public interface INewNameValue : INameValue
    {
        /// <summary>
        /// Requested new name.
        /// </summary>
        string NewName { get; set; }
    }
}
