namespace FoxIDs.Models.Logic
{
    public class LoginUserIdentifier : UserIdentifier
    {
        public string UserId { get; set; }

        /// <summary>
        /// User logged in with the user identifier which is equal to either the EmailIdentifier, PhoneIdentifier or UsernameIdentifier.
        /// </summary>
        public string UserIdentifier { get; set; }
    }
}
