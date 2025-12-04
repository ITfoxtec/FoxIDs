namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordMaxLengthException : PasswordPolicyException
    {
        public PasswordMaxLengthException() { }
        public PasswordMaxLengthException(string message) : base(message) { }
        public PasswordMaxLengthException(string message, System.Exception innerException) : base(message, innerException) { }
    }
}
