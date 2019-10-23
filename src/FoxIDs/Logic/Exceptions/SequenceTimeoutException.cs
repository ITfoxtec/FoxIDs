using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class SequenceTimeoutException : SequenceException
    {
        public SequenceTimeoutException() { }
        public SequenceTimeoutException(string message) : base(message) { }
        public SequenceTimeoutException(string message, Exception innerException) : base(message, innerException) { }
        protected SequenceTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}