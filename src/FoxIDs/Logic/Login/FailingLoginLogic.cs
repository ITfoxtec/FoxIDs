using Microsoft.AspNetCore.Http;
using FoxIDs.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Logic.Caches.Providers;

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

        public async Task<long> IncreaseFailingLoginCountAsync(string username, bool isExternalLogin = false)
        {
            var key = FailingLoginCountCacheKey(username, isExternalLogin);
            return await cacheProvider.IncrementNumberAsync(key, RouteBinding.FailingLoginCountLifetime);
        }

        public async Task ResetFailingLoginCountAsync(string username, bool isExternalLogin = false)
        {
            await cacheProvider.DeleteAsync(FailingLoginCountCacheKey(username, isExternalLogin));
        }

        public async Task<long> VerifyFailingLoginCountAsync(string username, bool isExternalLogin = false)
        {
            var key = FailingLoginCountCacheKey(username, isExternalLogin);

            if (await cacheProvider.ExistsAsync(FailingLoginLockedCacheKey(username, isExternalLogin)))
            {
                logger.ScopeTrace(() => $"{GetUserText(isExternalLogin)} '{username}' locked by observation period.", triggerEvent: true);
                throw new UserObservationPeriodException($"{GetUserText(isExternalLogin)} '{username}' locked by observation period.");
            }

            var failingLoginCount = await cacheProvider.GetNumberAsync(key);
            if (failingLoginCount >= RouteBinding.MaxFailingLogins)
            {
                await cacheProvider.SetFlagAsync(FailingLoginLockedCacheKey(username, isExternalLogin), RouteBinding.FailingLoginObservationPeriod);
                await cacheProvider.DeleteAsync(key);

                logger.ScopeTrace(() => $"Observation period started for {GetUserText(isExternalLogin).ToLower()} '{username}'.", scopeProperties: FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);
                throw new UserObservationPeriodException($"Observation period started for {GetUserText(isExternalLogin).ToLower()} '{username}'.");
            }
            return failingLoginCount;
        }

        private string GetUserText(bool isExternalLogin)
        {
            return isExternalLogin ? "External login user" : "User";
        }

        public Dictionary<string, string> FailingLoginCountDictonary(long failingLoginCount) =>
            failingLoginCount > 0 ? new Dictionary<string, string> { { Constants.Logs.FailingLoginCount, Convert.ToString(failingLoginCount) } } : null;


        private string FailingLoginCountCacheKey(string username, bool isExternalLogin)
        {
            return $"failing_login_count_{CacheSubKey(username, isExternalLogin)}";
        }

        private string FailingLoginLockedCacheKey(string username, bool isExternalLogin)
        {
            return $"failing_login_locked_{CacheSubKey(username, isExternalLogin)}";
        }

        private string CacheSubKey(string username, bool isExternalLogin)
        {
            return $"{RouteBinding.TenantNameDotTrackName}{(isExternalLogin ? "_external_login" : string.Empty)}_{username}";
        }
    }
}
