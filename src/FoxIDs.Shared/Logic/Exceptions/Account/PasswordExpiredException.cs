namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordExpiredException : PasswordPolicyException
    {
        public PasswordExpiredException() { }
        public PasswordExpiredException(string message) : base(message) { }
        public PasswordExpiredException(string message, System.Exception innerException) : base(message, innerException) { }
    }
}