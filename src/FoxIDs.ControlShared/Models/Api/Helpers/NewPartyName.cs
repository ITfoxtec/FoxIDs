namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Describes a new party name and direction for use in rename operations.
    /// </summary>
    public class NewPartyName
    {
        /// <summary>
        /// Proposed party name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Indicates if the party is upstream (true) or downstream (false).
        /// </summary>
        public bool IsUpParty { get; set; }
    }
}
