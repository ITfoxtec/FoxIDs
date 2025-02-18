namespace FoxIDs.Client.Models.ViewModels
{
    public static class UsageLogIncludeTypes
    {
        public const string Tenants = "Tenants";
        public const string Tracks = "Environments";   
        public const string Users = "Users";
        public const string Logins = "Logins";
        public const string TokenRequests = "Token requests";
        public const string Additional = "Additional";
        public const string ControlApi = "Control API";
        public const string ControlApiGets = "Control API gets";
        public const string ControlApiUpdates = "Control API updates";
        public const string Confirmation = "Confirmation";
        public const string ResetPassword = "Reset password";
        public const string Mfa = "MFA";
        public const string RealCount = "Real request count";
        public const string ExtraCount = "Extra trace count";
        public const string Sms = "SMS";
        public const string SmsPrice = "SMS country EUR price";
        public const string Email = "Email";
    }
}
