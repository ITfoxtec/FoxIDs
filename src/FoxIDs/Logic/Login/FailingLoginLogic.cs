using Microsoft.AspNetCore.Http;
using StackExchange.Redis;
using FoxIDs.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;

namespace FoxIDs.Logic
{
    public class FailingLoginLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IConnectionMultiplexer redisConnectionMultiplexer;

        public FailingLoginLogic(TelemetryScopedLogger logger, IConnectionMultiplexer redisConnectionMultiplexer, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.redisConnectionMultiplexer = redisConnectionMultiplexer;
        }

        public async Task<long> IncreaseFailingLoginCountAsync(string email)
        {
            var key = FailingLoginCountRadisKey(email);
            var db = redisConnectionMultiplexer.GetDatabase();
            var loginCount = await db.StringIncrementAsync(key);
            await db.KeyExpireAsync(key, TimeSpan.FromSeconds(RouteBinding.FailingLoginCountLifetime));
            return loginCount;
        }

        public async Task ResetFailingLoginCountAsync(string email)
        {
            var db = redisConnectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync(FailingLoginCountRadisKey(email));
        }

        public async Task<long> VerifyFailingLoginCountAsync(string email)
        {
            var key = FailingLoginCountRadisKey(email);
            var db = redisConnectionMultiplexer.GetDatabase();

            if (await db.KeyExistsAsync(FailingLoginLockedRadisKey(email)))
            {
                logger.ScopeTrace(() => $"User '{email}' locked by observation period.", triggerEvent: true);
                throw new UserObservationPeriodException($"User '{email}' locked by observation period.");
            }

            var failingLoginCount = (long?)await db.StringGetAsync(key);
            if (failingLoginCount.HasValue && failingLoginCount.Value >= RouteBinding.MaxFailingLogins)
            {
                await db.StringSetAsync(FailingLoginLockedRadisKey(email), true, TimeSpan.FromSeconds(RouteBinding.FailingLoginObservationPeriod));
                await db.KeyDeleteAsync(key);

                logger.ScopeTrace(() => $"Observation period started for user '{email}'.", scopeProperties: FailingLoginCountDictonary(failingLoginCount.Value), triggerEvent: true);
                throw new UserObservationPeriodException($"Observation period started for user '{email}'.");
            }
            return failingLoginCount.HasValue ? failingLoginCount.Value : 0;
        }

        public Dictionary<string, string> FailingLoginCountDictonary(long failingLoginCount) =>
            failingLoginCount > 0 ? new Dictionary<string, string> { { Constants.Logs.FailingLoginCount, Convert.ToString(failingLoginCount) } } : null;


        private string FailingLoginCountRadisKey(string email)
        {
            return $"failing_login_count_{RouteBinding.TenantNameDotTrackName}_{email}";
        }

        private string FailingLoginLockedRadisKey(string email)
        {
            return $"failing_login_locked_{RouteBinding.TenantNameDotTrackName}_{email}";
        }
    }
}
