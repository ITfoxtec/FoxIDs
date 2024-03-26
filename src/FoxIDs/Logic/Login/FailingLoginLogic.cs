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

        public async Task<long> IncreaseFailingLoginCountAsync(string email)
        {
            var key = FailingLoginCountCacheKey(email);

            return await cacheProvider.IncrementNumberAsync(key, RouteBinding.FailingLoginCountLifetime);
        }

        public async Task ResetFailingLoginCountAsync(string email)
        {
            await cacheProvider.DeleteAsync(FailingLoginCountCacheKey(email));
        }

        public async Task<long> VerifyFailingLoginCountAsync(string email)
        {
            var key = FailingLoginCountCacheKey(email);

            if (await cacheProvider.ExistsAsync(FailingLoginLockedCacheKey(email)))
            {
                logger.ScopeTrace(() => $"User '{email}' locked by observation period.", triggerEvent: true);
                throw new UserObservationPeriodException($"User '{email}' locked by observation period.");
            }

            var failingLoginCount = await cacheProvider.GetNumberAsync(key);
            if (failingLoginCount >= RouteBinding.MaxFailingLogins)
            {
                await cacheProvider.SetFlagAsync(FailingLoginLockedCacheKey(email), RouteBinding.FailingLoginObservationPeriod);
                await cacheProvider.DeleteAsync(key);

                logger.ScopeTrace(() => $"Observation period started for user '{email}'.", scopeProperties: FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);
                throw new UserObservationPeriodException($"Observation period started for user '{email}'.");
            }
            return failingLoginCount;
        }

        public Dictionary<string, string> FailingLoginCountDictonary(long failingLoginCount) =>
            failingLoginCount > 0 ? new Dictionary<string, string> { { Constants.Logs.FailingLoginCount, Convert.ToString(failingLoginCount) } } : null;


        private string FailingLoginCountCacheKey(string email)
        {
            return $"failing_login_count_{RouteBinding.TenantNameDotTrackName}_{email}";
        }

        private string FailingLoginLockedCacheKey(string email)
        {
            return $"failing_login_locked_{RouteBinding.TenantNameDotTrackName}_{email}";
        }
    }
}
