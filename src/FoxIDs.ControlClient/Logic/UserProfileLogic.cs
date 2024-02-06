using FoxIDs.Client.Services;
using FoxIDs.Infrastructure;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace FoxIDs.Client.Logic
{
    public class UserProfileLogic
    {
        [Inject]
        public UserService UserService { get; set; }

        public string userSub { get; set; }
        private UserControlProfile userControlProfile;

        public async Task<UserControlProfile> GetUserProfileAsync(string changeUserSub)
        {
            if (!changeUserSub.IsNullOrWhiteSpace())
            {
                userSub = changeUserSub;
            }

            if (userControlProfile == null)
            {
                try
                {
                    userControlProfile = await UserService.GetUserControlProfileAsync(userSub);
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
                userSub = userSub;
            });
            await UserService.UpdateUserControlProfileAsync(userControlProfileRequest);
        }
    }
}
