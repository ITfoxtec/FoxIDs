using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordUrlTextComplexityException : Exception
    {
        public PasswordUrlTextComplexityException() { }
        public PasswordUrlTextComplexityException(string message) : base(message) { }
        public PasswordUrlTextComplexityException(string message, Exception innerException) : base(message, innerException) { }
        protected PasswordUrlTextComplexityException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}