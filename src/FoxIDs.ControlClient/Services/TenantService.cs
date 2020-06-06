using FoxIDs.Infrastructure;
using FoxIDs.Models.Api;
using FoxIDs.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class TenantService
    {
        private const string apiUri = "api/master/master/!tenant";
        private const string filterApiUri = "api/master/master/!filtertenant";
        private readonly HttpClient httpClient;

        public TenantService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<IEnumerable<Tenant>> SearchTenantAsync(string filterName)
        {
            using var response = await httpClient.GetAsync($"{filterApiUri}?filterName={filterName}");
            var tenants = await response.ToObjectAsync<IEnumerable<Tenant>>();
            return tenants;

        }

        public async Task CreateTenantAsync(CreateTenantViewModel tenant)
        {
            using var response = await httpClient.PostAsJsonAsync(apiUri, new Tenant { Name = tenant.Name });
            var tenantResponse = await response.ToObjectAsync<Tenant>();
        }
    }
}
