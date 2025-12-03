namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordHistoryException : PasswordPolicyException
    {
        public PasswordHistoryException() { }
        public PasswordHistoryException(string message) : base(message) { }
        public PasswordHistoryException(string message, System.Exception innerException) : base(message, innerException) { }
    }
}