namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Types of actions captured in audit logs.
    /// </summary>
    public enum AuditTypes
    {
        Data = 100,
        Login = 200,
        Logout = 300,
        ChangePassword = 400,
        CreateUser = 500
    }
}
