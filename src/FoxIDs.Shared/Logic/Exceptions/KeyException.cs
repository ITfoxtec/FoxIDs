using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class KeyException : Exception
    {
        public KeyException() { }
        public KeyException(string message) : base(message) { }
        public KeyException(string message, Exception inner) : base(message, inner) { }
    }
}
