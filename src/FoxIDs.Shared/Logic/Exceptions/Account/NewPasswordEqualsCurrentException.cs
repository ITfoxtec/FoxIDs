using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class NewPasswordEqualsCurrentException : AccountException
    {
        public NewPasswordEqualsCurrentException() { }
        public NewPasswordEqualsCurrentException(string message) : base(message) { }
        public NewPasswordEqualsCurrentException(string message, Exception innerException) : base(message, innerException) { }
        protected NewPasswordEqualsCurrentException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}