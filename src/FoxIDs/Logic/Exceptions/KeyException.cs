using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class KeyException : Exception
    {
        public KeyException() { }
        public KeyException(string message) : base(message) { }
        public KeyException(string message, Exception inner) : base(message, inner) { }
        protected KeyException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
