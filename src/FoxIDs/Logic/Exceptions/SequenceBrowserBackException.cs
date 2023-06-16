using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class SequenceBrowserBackException : SequenceException
    {
        public SequenceBrowserBackException() { }
        public SequenceBrowserBackException(string message) : base(message) { }
        public SequenceBrowserBackException(string message, Exception innerException) : base(message, innerException) { }
        protected SequenceBrowserBackException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}