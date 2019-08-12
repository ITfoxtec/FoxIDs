using System;
using System.Runtime.Serialization;

namespace FoxIDs.Models.Config
{
    [Serializable]
    public class InvalidConfigException : Exception
    {
        public InvalidConfigException() { }
        public InvalidConfigException(string message) : base(message) { }
        public InvalidConfigException(string message, Exception inner) : base(message, inner) { }
        protected InvalidConfigException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
