namespace FoxIDs.Models.Logic
{
    public enum PasswordState
    {
        Current = 100, // Existing (entered for authentication)
        New = 200      // New password being set or changed to
    }
}