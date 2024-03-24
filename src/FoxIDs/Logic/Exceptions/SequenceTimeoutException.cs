using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class SequenceTimeoutException : SequenceException
    {
        public SequenceTimeoutException() { }
        public SequenceTimeoutException(string message) : base(message) { }
        public SequenceTimeoutException(string message, Exception innerException) : base(message, innerException) { }

        public bool? AccountAction { get; set; }
        public int SequenceLifetime { get; set; }
    }
}