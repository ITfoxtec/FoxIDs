using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class InvalidRecoveryCodeException : AccountException
    {
        public InvalidRecoveryCodeException() { }
        public InvalidRecoveryCodeException(string message) : base(message) { }
        public InvalidRecoveryCodeException(string message, Exception innerException) : base(message, innerException) { }
        protected InvalidRecoveryCodeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}