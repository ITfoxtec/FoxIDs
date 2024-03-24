using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class InvalidRecoveryCodeException : AccountException
    {
        public InvalidRecoveryCodeException() { }
        public InvalidRecoveryCodeException(string message) : base(message) { }
        public InvalidRecoveryCodeException(string message, Exception innerException) : base(message, innerException) { }
    }
}