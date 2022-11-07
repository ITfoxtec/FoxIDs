using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class PlanCacheLogic : LogicBase
    {
        private readonly Settings settings;
        private readonly IConnectionMultiplexer redisConnectionMultiplexer;
        private readonly IMasterRepository masterRepository;

        public PlanCacheLogic(Settings settings, IConnectionMultiplexer redisConnectionMultiplexer, IMasterRepository masterRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.redisConnectionMultiplexer = redisConnectionMultiplexer;
            this.masterRepository = masterRepository;
        }

        public async Task InvalidatePlanCacheAsync(string planName)
        {
            var key = RadisPlanNameKey(planName);
            var db = redisConnectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync(key);
        }

        public async Task<Plan> GetPlanAsync(string planName, bool required = true)
        {
            var key = RadisPlanNameKey(planName);
            var db = redisConnectionMultiplexer.GetDatabase();

            var planAsString = (string)await db.StringGetAsync(key);
            if (!planAsString.IsNullOrEmpty())
            {
                return planAsString.ToObject<Plan>();
            }

            var plan = await masterRepository.GetAsync<Plan>(await Plan.IdFormatAsync(planName), required: required);
            if (plan != null)
            {
                await db.StringSetAsync(key, plan.ToJson(), TimeSpan.FromSeconds(settings.Cache.PlanLifetime));
            }
            return plan;
        }

        private string RadisPlanNameKey(string planName)
        {
            return $"plan_cache_name_{planName}";
        }
    }
}
