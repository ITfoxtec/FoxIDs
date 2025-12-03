using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordComplexityException : PasswordPolicyException
    {
        public PasswordComplexityException() { }
        public PasswordComplexityException(string message) : base(message) { }
        public PasswordComplexityException(string message, Exception innerException) : base(message, innerException) { }
    }
}