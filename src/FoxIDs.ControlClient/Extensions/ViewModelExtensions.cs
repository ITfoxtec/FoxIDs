using FoxIDs.Client.Models.ViewModels;

namespace FoxIDs
{
    public static class ViewModelExtensions
    {
        public static bool RequirePassword(this UserViewModel user) => !(user.DisablePasswordAuth == true || user.SetPasswordSms || user.SetPasswordEmail);
    }
}
