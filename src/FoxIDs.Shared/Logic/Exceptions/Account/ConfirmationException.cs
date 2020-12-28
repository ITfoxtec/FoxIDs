using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class ConfirmationException : AccountActionException
    {
        public ConfirmationException() { }
        public ConfirmationException(string message) : base(message) { }
        public ConfirmationException(string message, Exception innerException) : base(message, innerException) { }
        protected ConfirmationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}