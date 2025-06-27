using FoxIDs.Models.Logic;
using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class UserExistsException : AccountException
    {
        public UserExistsException() { }
        public UserExistsException(string message) : base(message) { }
        public UserExistsException(string message, Exception innerException) : base(message, innerException) { }

        public UserIdentifier UserIdentifier { get; set; }
    }
}