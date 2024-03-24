using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordRiskException : AccountException
    {
        public PasswordRiskException() { }
        public PasswordRiskException(string message) : base(message) { }
        public PasswordRiskException(string message, Exception innerException) : base(message, innerException) { }
    }
}