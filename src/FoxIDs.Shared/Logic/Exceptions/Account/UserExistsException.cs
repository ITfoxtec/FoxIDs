using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class InvalidEmailException : AccountException
    {
        public InvalidEmailException() { }
        public InvalidEmailException(string message) : base(message) { }
        public InvalidEmailException(string message, Exception innerException) : base(message, innerException) { }
    }
}