using FoxIDs.Client.Services;
using FoxIDs.Infrastructure;
using FoxIDs.Models.Api;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Client.Logic
{
    public class UserProfileLogic
    {
        private UserControlProfile userControlProfile;
        private readonly UserService userService;

        public UserProfileLogic(UserService UserService)
        {
            userService = UserService;
        }

        public async Task<UserControlProfile> GetUserProfileAsync()
        {
            if (userControlProfile == null)
            {
                try
                {
                    userControlProfile = await userService.GetUserControlProfileAsync();
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
            if (userControlProfile != null && userControlProfile.LastTrackName == trackName)
            {
                return;
            }

            if (userControlProfile == null)
            {
                userControlProfile = new UserControlProfile();
            }

            userControlProfile.LastTrackName = trackName;
            await UpdateUserProfileAsync();
        }

        private async Task UpdateUserProfileAsync()
        {
            try
            {
                await userService.UpdateUserControlProfileAsync(userControlProfile);
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Console.WriteLine("Forbidden, you do not possess the required scope and role to update the user profile.");
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
