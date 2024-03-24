using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class UserNotExistsException : AccountException
    {
        public UserNotExistsException() { }
        public UserNotExistsException(string message) : base(message) { }
        public UserNotExistsException(string message, Exception innerException) : base(message, innerException) { }
    }
}