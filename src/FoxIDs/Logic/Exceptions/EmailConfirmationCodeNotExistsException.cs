using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class EmailConfirmationCodeNotExistsException : AccountException
    {
        public EmailConfirmationCodeNotExistsException() { }
        public EmailConfirmationCodeNotExistsException(string message) : base(message) { }
        public EmailConfirmationCodeNotExistsException(string message, Exception innerException) : base(message, innerException) { }
        protected EmailConfirmationCodeNotExistsException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}