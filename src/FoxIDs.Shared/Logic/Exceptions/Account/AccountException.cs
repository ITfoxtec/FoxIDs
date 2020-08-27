using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{    
    [Serializable]
    public class AccountException : Exception
    {
        public AccountException() { }
        public AccountException(string message) : base(message) { }
        public AccountException(string message, Exception inner) : base(message, inner) { }
        protected AccountException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}