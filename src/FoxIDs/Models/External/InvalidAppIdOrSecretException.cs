using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class InvalidAppIdOrSecretException : Exception
    {
        public InvalidAppIdOrSecretException() { }
        public InvalidAppIdOrSecretException(string message) : base(message) { }
        public InvalidAppIdOrSecretException(string message, Exception innerException) : base(message, innerException) { }
    }
}