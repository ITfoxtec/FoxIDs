using ITfoxtec.Identity.Helpers;
using FoxIDs.SeedDataTool.Model;
using System;
using System.Threading.Tasks;

namespace FoxIDs.SeedDataTool.Logic
{
    public class AccessLogic
    {
        private readonly SeedSettings settings;
        private readonly TokenHelper tokenHelper;

        public AccessLogic(SeedSettings settings, TokenHelper tokenHelper)
        {
            this.settings = settings;
            this.tokenHelper = tokenHelper;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            Console.WriteLine("Getting seed client access token.");
            var accessToken = await tokenHelper.GetAccessTokenWithClientCredentialsAsync(settings.ClientId, settings.ClientSecret, settings.RedirectUri, "foxids_api:master");            
            Console.WriteLine($"Access token {accessToken.Substring(0, 40)}...");
            return accessToken;
        }
    }
}
