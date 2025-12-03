using FoxIDs.Models.Logic;
using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class SoftChangePasswordException : PasswordPolicyException
    {
        public SoftChangePasswordException() { }
        public SoftChangePasswordException(string message) : base(message) { }
        public SoftChangePasswordException(string message, Exception innerException) : base(message, innerException) { }
    }
}