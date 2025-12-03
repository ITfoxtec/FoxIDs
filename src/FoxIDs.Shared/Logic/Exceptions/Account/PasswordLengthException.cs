using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordLengthException : PasswordPolicyException
    {
        public PasswordLengthException() { }
        public PasswordLengthException(string message) : base(message) { }
        public PasswordLengthException(string message, Exception innerException) : base(message, innerException) { }
    }
}