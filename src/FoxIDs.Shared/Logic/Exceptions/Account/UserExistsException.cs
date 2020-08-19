using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class InvalidEmailException : AccountException
    {
        public InvalidEmailException() { }
        public InvalidEmailException(string message) : base(message) { }
        public InvalidEmailException(string message, Exception innerException) : base(message, innerException) { }
        protected InvalidEmailException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}