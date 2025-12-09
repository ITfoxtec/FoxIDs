namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Identifies a named profile and its display-friendly metadata.
    /// </summary>
    public interface IProfile : INameValue, INewNameValue
    {
        /// <summary>
        /// Human readable profile title.
        /// </summary>
        string DisplayName { get; set; }
    }
}
