using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordEmailTextComplexityException : Exception
    {
        public PasswordEmailTextComplexityException() { }
        public PasswordEmailTextComplexityException(string message) : base(message) { }
        public PasswordEmailTextComplexityException(string message, Exception innerException) : base(message, innerException) { }
        protected PasswordEmailTextComplexityException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}