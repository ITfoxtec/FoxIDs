using System;

namespace FoxIDs.Logic
{    
    [Serializable]
    public class AccountActionException : Exception
    {
        public AccountActionException() { }
        public AccountActionException(string message) : base(message) { }
        public AccountActionException(string message, Exception inner) : base(message, inner) { }
    }
}