using FoxIDs.Models.Logic;

namespace FoxIDs.Logic
{
    public class PasswordBannedCharactersException : PasswordPolicyException
    {
        public PasswordBannedCharactersException() { }
        public PasswordBannedCharactersException(string message) : base(message) { }
        public PasswordBannedCharactersException(string message, System.Exception innerException) : base(message, innerException) { }
    }
}