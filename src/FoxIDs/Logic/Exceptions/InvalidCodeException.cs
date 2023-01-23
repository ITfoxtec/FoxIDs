using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class InvalidCodeException : AccountException
    {
        public InvalidCodeException() { }
        public InvalidCodeException(string message) : base(message) { }
        public InvalidCodeException(string message, Exception innerException) : base(message, innerException) { }
        protected InvalidCodeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}