using FoxIDs.Models.Logic;
using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordRiskException : PasswordPolicyException
    {
        public PasswordRiskException() { }
        public PasswordRiskException(string message) : base(message) { }
        public PasswordRiskException(string message, Exception innerException) : base(message, innerException) { }
    }
}