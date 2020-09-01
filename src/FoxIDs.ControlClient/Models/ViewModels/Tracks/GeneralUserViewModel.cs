using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralUserViewModel : User
    {
        public GeneralUserViewModel()
        { }

        public GeneralUserViewModel(User user)
        {
            Email = user.Email;
        }

        public bool Edit { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool CreateMode { get; set; }

        public bool DeleteAcknowledge { get; set; }

        public string Error { get; set; }

        public PageEditForm<UserViewModel> Form { get; set; }
    }
}
