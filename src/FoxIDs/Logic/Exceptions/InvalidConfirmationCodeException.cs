using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class InvalidConfirmationCodeException : AccountException
    {
        public InvalidConfirmationCodeException() { }
        public InvalidConfirmationCodeException(string message) : base(message) { }
        public InvalidConfirmationCodeException(string message, Exception innerException) : base(message, innerException) { }
        protected InvalidConfirmationCodeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}