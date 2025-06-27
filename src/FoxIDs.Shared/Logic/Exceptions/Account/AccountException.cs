using System;
using System.Collections.Generic;

namespace FoxIDs.Logic
{    
    [Serializable]
    public class AccountException : Exception
    {
        public AccountException() { }
        public AccountException(string message) : base(message) { }
        public AccountException(string message, Exception inner) : base(message, inner) { }

        public List<string> UiErrorMessages { get; set; }
    }
}