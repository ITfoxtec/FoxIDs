using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class InvalidCodeException : AccountException
    {
        public InvalidCodeException() { }
        public InvalidCodeException(string message) : base(message) { }
        public InvalidCodeException(string message, Exception innerException) : base(message, innerException) { }
    }
}