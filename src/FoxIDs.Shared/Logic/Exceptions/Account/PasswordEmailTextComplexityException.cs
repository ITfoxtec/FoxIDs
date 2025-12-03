using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordEmailTextComplexityException : PasswordPolicyException
    {
        public PasswordEmailTextComplexityException() { }
        public PasswordEmailTextComplexityException(string message) : base(message) { }
        public PasswordEmailTextComplexityException(string message, Exception innerException) : base(message, innerException) { }
    }
}