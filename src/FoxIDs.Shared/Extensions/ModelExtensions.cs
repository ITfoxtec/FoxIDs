using FoxIDs.Models;

namespace FoxIDs
{
    public static class ModelExtensions
    {
        public static bool RequirePassword(this User user) => !(user.DisablePasswordAuth == true || user.SetPasswordSms || user.SetPasswordEmail);
    }
}
