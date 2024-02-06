using FoxIDs.Client.Services;
using FoxIDs.Infrastructure;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System.Threading.Tasks;

namespace FoxIDs.Client.Logic
{
    public class UserProfileLogic
    {
        private string userSub;
        private UserControlProfile userControlProfile;
        private readonly UserService userService;

        public UserProfileLogic(UserService UserService)
        {
            userService = UserService;
        }

        public async Task<UserControlProfile> GetUserProfileAsync(string userSub)
        {
            if (!userSub.IsNullOrWhiteSpace())
            {
                this.userSub = userSub;
            }

            if (userControlProfile == null)
            {
                try
                {
                    userControlProfile = await userService.GetUserControlProfileAsync(this.userSub);
                }
                catch (FoxIDsApiException ex)
                {
                    if (ex.StatusCode != System.Net.HttpStatusCode.NotFound)
                    {
                        throw;
                    }
                }
            }
             
            return userControlProfile;
        }

        public async Task UpdateTrackAsync(string trackName)
        {
            if(userControlProfile == null)
            {
                userControlProfile = new UserControlProfile();
            }

            userControlProfile.LastTrackName = trackName;
            await UpdateUserProfileAsync();
        }

        private async Task UpdateUserProfileAsync()
        {
            var userControlProfileRequest = userControlProfile.Map<UserControlProfileRequest>(afterMap => 
            {
                afterMap.UserSub = userSub;
            });
            await userService.UpdateUserControlProfileAsync(userControlProfileRequest);
        }
    }
}
