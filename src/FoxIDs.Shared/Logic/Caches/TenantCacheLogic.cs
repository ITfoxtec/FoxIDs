using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class TenantCacheLogic : LogicBase
    {
        private readonly Settings settings;
        private readonly IConnectionMultiplexer redisConnectionMultiplexer;
        private readonly ITenantRepository tenantRepository;

        public TenantCacheLogic(Settings settings, IConnectionMultiplexer redisConnectionMultiplexer, ITenantRepository tenantRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.redisConnectionMultiplexer = redisConnectionMultiplexer;
            this.tenantRepository = tenantRepository;
        }

        public async Task InvalidateCustomDomainCacheAsync(string customDomain)
        {
            var key = RadisTenantCustomDomainKey(customDomain);
            var db = redisConnectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync(key);
        }

        public async Task<string> GetTenantNameByCustomDomainAsync(string customDomain)
        {
            var key = RadisTenantCustomDomainKey(customDomain);
            var db = redisConnectionMultiplexer.GetDatabase();

            var tenantName = (string)await db.StringGetAsync(key);
            if (!tenantName.IsNullOrEmpty())
            {
                return tenantName;
            }

            var tenant = await GetTenantByCustomDomainAsync(customDomain);
            await db.StringSetAsync(key, tenant.Name, TimeSpan.FromSeconds(settings.Cache.CustomDomainLifetime));
            return tenant.Name;
        }

        private async Task<Tenant> GetTenantByCustomDomainAsync(string customDomain)
        {
            try
            {
                (var tenants, _) = await tenantRepository.GetListAsync<Tenant>(whereQuery: t => t.CustomDomain.Equals(customDomain, StringComparison.OrdinalIgnoreCase) && t.CustomDomainVerified);
                return tenants.First();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new Exception($"Custom domain '{customDomain}' is not connected to a tenant.", ex);
                }

                throw new Exception($"Unable to find tenant by custom domain '{customDomain}'.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unknown custom domain '{customDomain}' is not connected to a tenant.", ex);
            }
        }

        private string RadisTenantCustomDomainKey(string customDomain)
        {
            return $"tenant_cache_customdomain_{customDomain}";
        }
    }
}
