using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordUsernameTextComplexityException : AccountException
    {
        public PasswordUsernameTextComplexityException() { }
        public PasswordUsernameTextComplexityException(string message) : base(message) { }
        public PasswordUsernameTextComplexityException(string message, Exception innerException) : base(message, innerException) { }
    }
}