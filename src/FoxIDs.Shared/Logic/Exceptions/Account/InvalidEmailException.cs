using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class UserExistsException : AccountException
    {
        public UserExistsException() { }
        public UserExistsException(string message) : base(message) { }
        public UserExistsException(string message, Exception innerException) : base(message, innerException) { }
        protected UserExistsException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public string Email { get; set; }
    }
}