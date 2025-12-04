using FoxIDs.Models.Logic;
using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordUrlTextComplexityException : PasswordPolicyException
    {
        public PasswordUrlTextComplexityException() { }
        public PasswordUrlTextComplexityException(string message) : base(message) { }
        public PasswordUrlTextComplexityException(string message, Exception innerException) : base(message, innerException) { }
    }
}