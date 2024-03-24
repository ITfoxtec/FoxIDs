using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class ExternalKeyIsNotReadyException : Exception
    {
        public ExternalKeyIsNotReadyException() { }
        public ExternalKeyIsNotReadyException(string message) : base(message) { }
        public ExternalKeyIsNotReadyException(string message, Exception inner) : base(message, inner) { }
    }
}
