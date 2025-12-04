using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordPhoneTextComplexityException : PasswordPolicyException
    {
        public PasswordPhoneTextComplexityException() { }
        public PasswordPhoneTextComplexityException(string message) : base(message) { }
        public PasswordPhoneTextComplexityException(string message, Exception innerException) : base(message, innerException) { }
    }
}