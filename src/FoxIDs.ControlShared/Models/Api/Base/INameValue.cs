namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Provides a named value used when addressing API resources.
    /// </summary>
    public interface INameValue
    {
        /// <summary>
        /// Resource name component.
        /// </summary>
        string Name { get; set; }
    }
}
