using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class ResetPasswordException : AccountActionException
    {
        public ResetPasswordException() { }
        public ResetPasswordException(string message) : base(message) { }
        public ResetPasswordException(string message, Exception innerException) : base(message, innerException) { }
        protected ResetPasswordException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}