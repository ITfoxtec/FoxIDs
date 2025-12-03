using FoxIDs.Models.Logic;
using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordPolicyException : AccountException
    {
        public PasswordPolicyException() { }
        public PasswordPolicyException(string message) : base(message) { }
        public PasswordPolicyException(string message, Exception inner) : base(message, inner) { }

        public PasswordPolicyState PasswordPolicy { get; set; }
    }
}