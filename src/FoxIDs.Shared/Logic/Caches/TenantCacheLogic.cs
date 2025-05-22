using FoxIDs.Logic.Caches.Providers;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class TenantCacheLogic : LogicBase
    {
        private readonly Settings settings;
        private readonly IDataCacheProvider cacheProvider;
        private readonly ITenantDataRepository tenantDataRepository;

        public TenantCacheLogic(Settings settings, IDataCacheProvider cacheProvider, ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.cacheProvider = cacheProvider;
            this.tenantDataRepository = tenantDataRepository;
        }

        public async Task InvalidateTenantCacheAsync(string tenantName)
        {
            var key = CacheTenantNameKey(tenantName);
            await cacheProvider.DeleteAsync(key);
        }

        public async Task InvalidateCustomDomainCacheAsync(string customDomain)
        {
            var key = CacheTenantCustomDomainKey(customDomain);
            await cacheProvider.DeleteAsync(key);
        }

        public async Task<Tenant> GetTenantAsync(string tenantName)
        {
            var key = CacheTenantNameKey(tenantName);

            var tenantAsString = await cacheProvider.GetAsync(key);
            if (!tenantAsString.IsNullOrEmpty())
            {
                return tenantAsString.ToObject<Tenant>();
            }

            var tenant = await tenantDataRepository.GetAsync<Tenant>(await Tenant.IdFormatAsync(tenantName));
            if (tenant.ForUsage == true)
            {
                throw new FoxIDsDataException(tenant.Id, tenant.PartitionId) { StatusCode = DataStatusCode.NotFound };
            }
            if (tenant != null)
            {
                await cacheProvider.SetAsync(key, tenant.ToJson(), settings.Cache.TenantLifetime);
            }
            return tenant;
        }

        public async Task<Tenant> GetTenantByCustomDomainAsync(string customDomain)
        {
            var key = CacheTenantCustomDomainKey(customDomain);

            var tenantAsString = await cacheProvider.GetAsync(key);
            if (!tenantAsString.IsNullOrEmpty())
            {
                return tenantAsString.ToObject<Tenant>();
            }

            var tenant = await LoadTenantFromDbByCustomDomainAsync(customDomain);
            await cacheProvider.SetAsync(key, tenant.ToJson(), settings.Cache.TenantLifetime);
            return tenant;
        }

        private async Task<Tenant> LoadTenantFromDbByCustomDomainAsync(string customDomain)
        {
            try
            {
                (var tenants, _) = await tenantDataRepository.GetManyAsync<Tenant>(whereQuery: t => !(t.ForUsage == true) && t.CustomDomain.Equals(customDomain, StringComparison.CurrentCultureIgnoreCase) && t.CustomDomainVerified);
                return tenants.First();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
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

        private string CacheTenantNameKey(string tenantName)
        {
            return $"tenant_cache_name_{tenantName}";
        }

        private string CacheTenantCustomDomainKey(string customDomain)
        {
            return $"tenant_cache_customdomain_{customDomain}";
        }
    }
}
