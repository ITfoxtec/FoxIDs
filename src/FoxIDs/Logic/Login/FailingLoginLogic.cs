using Microsoft.AspNetCore.Http;
using FoxIDs.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Logic.Caches.Providers;
using FoxIDs.Repository;

namespace FoxIDs.Logic
{
    public class FailingLoginLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly ICacheProvider cacheProvider;

        public FailingLoginLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, ICacheProvider cacheProvider, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.cacheProvider = cacheProvider;
        }

        public async Task<long> IncreaseFailingLoginCountAsync(string userIdentifier, FailingLoginTypes failingLoginType)
        {
            var key = FailingLoginCountCacheKey(userIdentifier, failingLoginType);
            return await cacheProvider.IncrementNumberAsync(key, RouteBinding.FailingLoginCountLifetime);
        }

        public async Task ResetFailingLoginCountAsync(string userIdentifier, FailingLoginTypes failingLoginType)
        {
            await cacheProvider.DeleteAsync(FailingLoginCountCacheKey(userIdentifier, failingLoginType));
        }

        public async Task<long> VerifyFailingLoginCountAsync(string userIdentifier, FailingLoginTypes failingLoginType)
        {
            var failingLoginId = await FailingLoginLock.IdFormatAsync(new FailingLoginLock.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, UserIdentifier = userIdentifier, FailingLoginType = failingLoginType });
            var failingLogin = await tenantDataRepository.GetAsync<FailingLoginLock>(failingLoginId, required: false);
            if (failingLogin != null)
            {
                logger.ScopeTrace(() => $"{GetFailingLoginTypeText(failingLoginType)} '{userIdentifier}' locked by observation period.", triggerEvent: true);
                throw new UserObservationPeriodException($"{GetFailingLoginTypeText(failingLoginType)} '{userIdentifier}' locked by observation period.");
            }

            var key = FailingLoginCountCacheKey(userIdentifier, failingLoginType);
            var failingLoginCount = await cacheProvider.GetNumberAsync(key);
            if (failingLoginCount >= RouteBinding.MaxFailingLogins)
            {
                var newFailingLogin = new FailingLoginLock
                {
                    Id = failingLoginId,
                    UserIdentifier = userIdentifier,
                    FailingLoginType = failingLoginType,
                    CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    TimeToLive = RouteBinding.FailingLoginObservationPeriod
                };
                await tenantDataRepository.SaveAsync(newFailingLogin);
                await cacheProvider.DeleteAsync(key);

                logger.ScopeTrace(() => $"Observation period started for {GetFailingLoginTypeText(failingLoginType).ToLower()} '{userIdentifier}'.", scopeProperties: FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);
                throw new UserObservationPeriodException($"Observation period started for {GetFailingLoginTypeText(failingLoginType).ToLower()} '{userIdentifier}'.");
            }
            return failingLoginCount;
        }

        private string GetFailingLoginTypeText(FailingLoginTypes failingLoginType)
        {
            switch (failingLoginType)
            {
                case FailingLoginTypes.InternalLogin:
                    return "Internal login user";
                case FailingLoginTypes.ExternalLogin:
                    return "External login user";
                case FailingLoginTypes.SmsCode:
                    return "SMS code";
                case FailingLoginTypes.EmailCode:
                    return "Email code";
                case FailingLoginTypes.TwoFactorSmsCode:
                    return "SMS two-factor code";
                case FailingLoginTypes.TwoFactorEmailCode:
                    return "Email two-factor code";
                case FailingLoginTypes.TwoFactorAuthenticator:
                    return "Two-factor authenticator app";
                default:
                    throw new NotImplementedException();
            }
        }

        public Dictionary<string, string> FailingLoginCountDictonary(long failingLoginCount) =>
            failingLoginCount > 0 ? new Dictionary<string, string> { { Constants.Logs.FailingLoginCount, Convert.ToString(failingLoginCount) } } : null;


        private string FailingLoginCountCacheKey(string userIdentifier, FailingLoginTypes failingLoginType)
        {
            return $"failing_login_count_{CacheSubKey(userIdentifier, failingLoginType)}";
        }

        private string CacheSubKey(string userIdentifier, FailingLoginTypes failingLoginType)
        {
            return $"{RouteBinding.TenantNameDotTrackName}_{userIdentifier}_{(int)failingLoginType}";
        }
    }
}
