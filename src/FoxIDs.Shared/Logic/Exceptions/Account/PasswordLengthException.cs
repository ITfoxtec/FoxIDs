using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordLengthException : AccountException
    {
        public PasswordLengthException() { }
        public PasswordLengthException(string message) : base(message) { }
        public PasswordLengthException(string message, Exception innerException) : base(message, innerException) { }
    }
}