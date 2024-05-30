using FoxIDs.Logic.Caches.Providers;
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
        private readonly IDataCacheProvider cacheProvider;
        private readonly IMasterDataRepository masterDataRepository;

        public PlanCacheLogic(Settings settings, IDataCacheProvider cacheProvider, IMasterDataRepository masterDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.cacheProvider = cacheProvider;
            this.masterDataRepository = masterDataRepository;
        }

        public async Task InvalidatePlanCacheAsync(string planName)
        {
            var key = CachePlanNameKey(planName);
            await cacheProvider.DeleteAsync(key);
        }

        public async Task<Plan> GetPlanAsync(string planName, bool required = true)
        {
            var key = CachePlanNameKey(planName);
            var planAsString = await cacheProvider.GetAsync(key);
            if (!planAsString.IsNullOrEmpty())
            {
                return planAsString.ToObject<Plan>();
            }

            var plan = await masterDataRepository.GetAsync<Plan>(await Plan.IdFormatAsync(planName), required: required);
            if (plan != null)
            {
                await cacheProvider.SetAsync(key, plan.ToJson(), settings.Cache.PlanLifetime);
            }
            return plan;
        }

        private string CachePlanNameKey(string planName)
        {
            return $"plan_cache_name_{planName}";
        }
    }
}
