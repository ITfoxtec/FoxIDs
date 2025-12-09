namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Adds a nested user creation payload to a request.
    /// </summary>
    public interface ICreateUserRef
    {
        /// <summary>
        /// Details required to provision the user.
        /// </summary>
        public CreateUser CreateUser { get; set; }
    }
}
