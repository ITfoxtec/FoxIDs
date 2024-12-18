using Microsoft.AspNetCore.Http;
using FoxIDs.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Logic.Caches.Providers;
using FoxIDs.Models.Logic;
namespace FoxIDs.Logic
{
    public class FailingLoginLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ICacheProvider cacheProvider;

        public FailingLoginLogic(TelemetryScopedLogger logger, ICacheProvider cacheProvider, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.cacheProvider = cacheProvider;
        }

        public async Task<long> IncreaseFailingLoginCountAsync(string username, FailingLoginTypes failingLoginType)
        {
            var key = FailingLoginCountCacheKey(username, failingLoginType);
            return await cacheProvider.IncrementNumberAsync(key, RouteBinding.FailingLoginCountLifetime);
        }

        public async Task ResetFailingLoginCountAsync(string username, FailingLoginTypes failingLoginType)
        {
            await cacheProvider.DeleteAsync(FailingLoginCountCacheKey(username, failingLoginType));
        }

        public async Task<long> VerifyFailingLoginCountAsync(string username, FailingLoginTypes failingLoginType)
        {
            var key = FailingLoginCountCacheKey(username, failingLoginType);

            if (await cacheProvider.ExistsAsync(FailingLoginLockedCacheKey(username, failingLoginType)))
            {
                logger.ScopeTrace(() => $"{GetUserText(failingLoginType)} '{username}' locked by observation period.", triggerEvent: true);
                throw new UserObservationPeriodException($"{GetUserText(failingLoginType)} '{username}' locked by observation period.");
            }

            var failingLoginCount = await cacheProvider.GetNumberAsync(key);
            if (failingLoginCount >= RouteBinding.MaxFailingLogins)
            {
                await cacheProvider.SetFlagAsync(FailingLoginLockedCacheKey(username, failingLoginType), RouteBinding.FailingLoginObservationPeriod);
                await cacheProvider.DeleteAsync(key);

                logger.ScopeTrace(() => $"Observation period started for {GetUserText(failingLoginType).ToLower()} '{username}'.", scopeProperties: FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);
                throw new UserObservationPeriodException($"Observation period started for {GetUserText(failingLoginType).ToLower()} '{username}'.");
            }
            return failingLoginCount;
        }

        private string GetUserText(FailingLoginTypes failingLoginType)
        {
            switch (failingLoginType)
            {
                case FailingLoginTypes.Login:
                    return "User";
                case FailingLoginTypes.ExternalLogin:
                    return "External login user";
                case FailingLoginTypes.EmailCode:
                    return "Email code";
                case FailingLoginTypes.TwoFactorAuthenticator:
                    return "Two-factor authenticator";
                default:
                    throw new NotImplementedException();
            }
        }

        public Dictionary<string, string> FailingLoginCountDictonary(long failingLoginCount) =>
            failingLoginCount > 0 ? new Dictionary<string, string> { { Constants.Logs.FailingLoginCount, Convert.ToString(failingLoginCount) } } : null;


        private string FailingLoginCountCacheKey(string username, FailingLoginTypes failingLoginType)
        {
            return $"failing_login_count_{CacheSubKey(username, failingLoginType)}";
        }

        private string FailingLoginLockedCacheKey(string username, FailingLoginTypes failingLoginType)
        {
            return $"failing_login_locked_{CacheSubKey(username, failingLoginType)}";
        }

        private string CacheSubKey(string username, FailingLoginTypes failingLoginType)
        {
            return $"{RouteBinding.TenantNameDotTrackName}{CacheSubKey(failingLoginType)}_{username}";
        }

        private string CacheSubKey(FailingLoginTypes failingLoginType)
        {
            switch (failingLoginType)
            {
                case FailingLoginTypes.Login:
                    return string.Empty;
                case FailingLoginTypes.ExternalLogin:
                    return "_external_login";
                case FailingLoginTypes.EmailCode:
                    return "_email_code";
                case FailingLoginTypes.TwoFactorAuthenticator:
                    return "_mfa_authenticator";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
