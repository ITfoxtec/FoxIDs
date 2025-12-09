namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Includes information required to link a user to an external identity.
    /// </summary>
    public interface ILinkExternalUserRef
    {
        /// <summary>
        /// External account link details.
        /// </summary>
        public LinkExternalUser LinkExternalUser { get; set; }
    }
}
