using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class MfaException : AccountException
    {
        public MfaException() { }
        public MfaException(string message) : base(message) { }
        public MfaException(string message, Exception innerException) : base(message, innerException) { }
    }
}
