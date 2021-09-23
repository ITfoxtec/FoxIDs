using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class EmailConfigurationException : Exception
    {
        public EmailConfigurationException() { }
        public EmailConfigurationException(string message) : base(message) { }
        public EmailConfigurationException(string message, Exception inner) : base(message, inner) { }
        protected EmailConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
