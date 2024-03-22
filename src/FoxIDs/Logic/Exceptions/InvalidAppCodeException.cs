using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class InvalidAppCodeException : AccountException
    {
        public InvalidAppCodeException() { }
        public InvalidAppCodeException(string message) : base(message) { }
        public InvalidAppCodeException(string message, Exception innerException) : base(message, innerException) { }
    }
}