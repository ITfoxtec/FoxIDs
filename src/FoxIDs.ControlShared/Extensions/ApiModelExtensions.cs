using FoxIDs.Models.Api;

namespace FoxIDs
{
    public static class ApiModelExtensions
    {
        public static bool RequirePassword(this CreateUserRequest user) => !(user.DisablePasswordAuth == true || user.SetPasswordSms || user.SetPasswordEmail);
    }
}
