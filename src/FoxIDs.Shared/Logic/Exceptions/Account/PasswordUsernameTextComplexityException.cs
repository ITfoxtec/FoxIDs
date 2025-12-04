using FoxIDs.Models.Logic;
using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordUsernameTextComplexityException : PasswordPolicyException
    {
        public PasswordUsernameTextComplexityException() { }
        public PasswordUsernameTextComplexityException(string message) : base(message) { }
        public PasswordUsernameTextComplexityException(string message, Exception innerException) : base(message, innerException) { }
    }
}