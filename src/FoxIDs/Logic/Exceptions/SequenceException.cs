using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class SequenceException : Exception
    {
        public SequenceException() { }
        public SequenceException(string message) : base(message) { }
        public SequenceException(string message, Exception innerException) : base(message, innerException) { }
    }
}