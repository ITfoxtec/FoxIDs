﻿using ITfoxtec.Identity.Helpers;
using FoxIDs.SeedTool.Models;
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
                Console.WriteLine("Getting seed client access token.");
                (var accessToken, var expiresIn) = await tokenHelper.GetAccessTokenWithClientCredentialGrantAsync(settings.ClientId, settings.ClientSecret, settings.Scope);
                accessTokenCache = accessToken;
                cacheExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (expiresIn.HasValue ? expiresIn.Value : 0);
                Console.WriteLine($"Access token {accessToken.Substring(0, 40)}...");
            }
            return accessTokenCache;
        }
    }
}
