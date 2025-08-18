using System;

namespace FoxIDs.Logic
{
    public class PasswordNotAcceptedExternalException : AccountException
    {
        public PasswordNotAcceptedExternalException() { }
        public PasswordNotAcceptedExternalException(string message) : base(message) { }
        public PasswordNotAcceptedExternalException(string message, Exception innerException) : base(message, innerException) { }
    }
}
