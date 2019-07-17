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
        private string accessTokenCache;
        private long cacheExpiresAt;

        public AccessLogic(SeedSettings settings, TokenHelper tokenHelper)
        {
            this.settings = settings;
            this.tokenHelper = tokenHelper;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (cacheExpiresAt < DateTimeOffset.UtcNow.AddSeconds(5).ToUnixTimeSeconds())
            {
                Console.WriteLine("Getting seed client access token.");
                (var accessToken, var expiresIn) = await tokenHelper.GetAccessTokenWithClientCredentialsAsync(settings.ClientId, settings.ClientSecret, settings.RedirectUri, "foxids_api:master");
                accessTokenCache = accessToken;
                cacheExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expiresIn;
                Console.WriteLine($"Access token {accessToken.Substring(0, 40)}...");
            }
            return accessTokenCache;
        }
    }
}
