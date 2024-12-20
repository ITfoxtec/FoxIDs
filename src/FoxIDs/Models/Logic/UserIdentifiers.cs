namespace FoxIDs.Models.Logic
{
    public class UserIdentifiers
    {
        public string UserId { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Username { get; set; }

        /// <summary>
        /// User logged in with the user identifier which is equal to either the EmailIdentifier, PhoneIdentifier or UsernameIdentifier.
        /// </summary>
        public string UserIdentifier { get; set; }
    }
}
