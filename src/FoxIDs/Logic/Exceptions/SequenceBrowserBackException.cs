using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class SequenceBrowserBackException : SequenceException
    {
        public SequenceBrowserBackException() { }
        public SequenceBrowserBackException(string message) : base(message) { }
        public SequenceBrowserBackException(string message, Exception innerException) : base(message, innerException) { }
    }
}