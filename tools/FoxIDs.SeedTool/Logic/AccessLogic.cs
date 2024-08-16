using ITfoxtec.Identity.Helpers;
using FoxIDs.SeedTool.Model;
using System;
using System.Threading.Tasks;

namespace FoxIDs.SeedTool.Logic
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
            if (cacheExpiresAt < DateTimeOffset.UtcNow.AddSeconds(-5).ToUnixTimeSeconds())
            {
                Console.WriteLine($"{Environment.NewLine}Getting seed client access token.");
                (var accessToken, var expiresIn) = await tokenHelper.GetAccessTokenWithClientCredentialGrantAsync(settings.ClientId, settings.ClientSecret, "foxids_control_api:foxids:master");
                accessTokenCache = accessToken;
                cacheExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (expiresIn.HasValue ? expiresIn.Value : 0);
                Console.WriteLine($"{Environment.NewLine}Access token {accessToken.Substring(0, 40)}...");
            }
            return accessTokenCache;
        }
    }
}
