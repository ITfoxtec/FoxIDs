using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordRiskException : Exception
    {
        public PasswordRiskException() { }
        public PasswordRiskException(string message) : base(message) { }
        public PasswordRiskException(string message, Exception innerException) : base(message, innerException) { }
        protected PasswordRiskException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}