using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class SequenceException : Exception
    {
        public SequenceException()
        {
        }

        public SequenceException(string message) : base(message)
        {
        }

        public SequenceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SequenceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}