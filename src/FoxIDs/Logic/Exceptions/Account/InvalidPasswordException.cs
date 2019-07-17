using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class InvalidPasswordException : AccountException
    {
        public InvalidPasswordException() { }
        public InvalidPasswordException(string message) : base(message) { }
        public InvalidPasswordException(string message, Exception innerException) : base(message, innerException) { }
        protected InvalidPasswordException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}