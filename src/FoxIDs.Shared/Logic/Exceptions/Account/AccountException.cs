using System;

namespace FoxIDs.Logic
{    
    [Serializable]
    public class AccountException : Exception
    {
        public AccountException() { }
        public AccountException(string message) : base(message) { }
        public AccountException(string message, Exception inner) : base(message, inner) { }
    }
}