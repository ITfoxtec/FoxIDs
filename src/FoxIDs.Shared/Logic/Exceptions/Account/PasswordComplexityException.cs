using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordComplexityException : AccountException
    {
        public PasswordComplexityException() { }
        public PasswordComplexityException(string message) : base(message) { }
        public PasswordComplexityException(string message, Exception innerException) : base(message, innerException) { }
    }
}