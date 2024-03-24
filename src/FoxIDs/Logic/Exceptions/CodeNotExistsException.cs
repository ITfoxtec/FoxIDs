using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class CodeNotExistsException : AccountException
    {
        public CodeNotExistsException() { }
        public CodeNotExistsException(string message) : base(message) { }
        public CodeNotExistsException(string message, Exception innerException) : base(message, innerException) { }
    }
}