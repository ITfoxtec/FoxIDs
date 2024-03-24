using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordUrlTextComplexityException : AccountException
    {
        public PasswordUrlTextComplexityException() { }
        public PasswordUrlTextComplexityException(string message) : base(message) { }
        public PasswordUrlTextComplexityException(string message, Exception innerException) : base(message, innerException) { }
    }
}