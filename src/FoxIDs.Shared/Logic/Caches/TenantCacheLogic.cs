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

        public async Task InvalidateTenantCacheAsync(string tenantName)
        {
            var key = RadisTenantNameKey(tenantName);
            var db = redisConnectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync(key);
        }

        public async Task InvalidateCustomDomainCacheAsync(string customDomain)
        {
            var key = RadisTenantCustomDomainKey(customDomain);
            var db = redisConnectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync(key);
        }

        public async Task<Tenant> GetTenantAsync(string tenantName, bool required = true)
        {
            var key = RadisTenantNameKey(tenantName);
            var db = redisConnectionMultiplexer.GetDatabase();

            var tenantAsString = (string)await db.StringGetAsync(key);
            if (!tenantAsString.IsNullOrEmpty())
            {
                return tenantAsString.ToObject<Tenant>();
            }

            var tenant = await tenantRepository.GetAsync<Tenant>(await Tenant.IdFormatAsync(tenantName), required: required);
            if (tenant != null)
            {
                await db.StringSetAsync(key, tenant.ToJson(), TimeSpan.FromSeconds(settings.Cache.TenantLifetime));
            }
            return tenant;
        }

        public async Task<Tenant> GetTenantByCustomDomainAsync(string customDomain)
        {
            var key = RadisTenantCustomDomainKey(customDomain);
            var db = redisConnectionMultiplexer.GetDatabase();

            var tenantAsString = (string)await db.StringGetAsync(key);
            if (!tenantAsString.IsNullOrEmpty())
            {
                return tenantAsString.ToObject<Tenant>();
            }

            var tenant = await LoadTenantFromDbByCustomDomainAsync(customDomain);
            await db.StringSetAsync(key, tenant.ToJson(), TimeSpan.FromSeconds(settings.Cache.TenantLifetime));
            return tenant;
        }

        private async Task<Tenant> LoadTenantFromDbByCustomDomainAsync(string customDomain)
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

        private string RadisTenantNameKey(string tenantName)
        {
            return $"tenant_cache_name_{tenantName}";
        }

        private string RadisTenantCustomDomainKey(string customDomain)
        {
            return $"tenant_cache_customdomain_{customDomain}";
        }
    }
}
