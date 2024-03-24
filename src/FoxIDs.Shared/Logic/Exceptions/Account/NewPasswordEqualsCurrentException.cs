using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class NewPasswordEqualsCurrentException : AccountException
    {
        public NewPasswordEqualsCurrentException() { }
        public NewPasswordEqualsCurrentException(string message) : base(message) { }
        public NewPasswordEqualsCurrentException(string message, Exception innerException) : base(message, innerException) { }
    }
}