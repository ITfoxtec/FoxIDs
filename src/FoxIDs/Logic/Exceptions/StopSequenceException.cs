using System;

namespace FoxIDs.Logic
{
    public class StopSequenceException : Exception
    {
        public StopSequenceException() { }
        public StopSequenceException(string message) : base(message) { }
        public StopSequenceException(string message, Exception inner) : base(message, inner) { }
    }
}
