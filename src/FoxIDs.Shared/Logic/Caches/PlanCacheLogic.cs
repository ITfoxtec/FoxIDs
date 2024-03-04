using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class PlanCacheLogic : LogicBase
    {
        private readonly Settings settings;
        private readonly IDistributedCacheProvider cacheProvider;
        private readonly IMasterRepository masterRepository;

        public PlanCacheLogic(Settings settings, IDistributedCacheProvider cacheProvider, IMasterRepository masterRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.cacheProvider = cacheProvider;
            this.masterRepository = masterRepository;
        }

        public async Task InvalidatePlanCacheAsync(string planName)
        {
            var key = RadisPlanNameKey(planName);
            await cacheProvider.DeleteAsync(key);
        }

        public async Task<Plan> GetPlanAsync(string planName, bool required = true)
        {
            var key = RadisPlanNameKey(planName);
            var planAsString = (string)await cacheProvider.GetAsync(key);
            if (!planAsString.IsNullOrEmpty())
            {
                return planAsString.ToObject<Plan>();
            }

            var plan = await masterRepository.GetAsync<Plan>(await Plan.IdFormatAsync(planName), required: required);
            if (plan != null)
            {
                await cacheProvider.SetAsync(key, plan.ToJson(), settings.Cache.PlanLifetime);
            }
            return plan;
        }

        private string RadisPlanNameKey(string planName)
        {
            return $"plan_cache_name_{planName}";
        }
    }
}
