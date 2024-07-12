using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class InvalidUsernameOrPasswordException : AccountException
    {
        public InvalidUsernameOrPasswordException() { }
        public InvalidUsernameOrPasswordException(string message) : base(message) { }
        public InvalidUsernameOrPasswordException(string message, Exception innerException) : base(message, innerException) { }
    }
}