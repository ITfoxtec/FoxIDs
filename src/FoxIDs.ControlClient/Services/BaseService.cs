using FoxIDs.Client.Logic;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public abstract class BaseService
    {
        private readonly HttpClient httpClient;
        private readonly RouteBindingLogic routeBindingLogic;

        public BaseService(HttpClient httpClient, RouteBindingLogic routeBindingLogic)
        {
            this.httpClient = httpClient;
            this.routeBindingLogic = routeBindingLogic;
        }

        protected async Task<string> GetTenantApiUrlAsync(string url, string tenantName = null)
        {
            tenantName = tenantName ?? await routeBindingLogic.GetTenantNameAsync();
            return url.Replace("{tenant}", tenantName);
        }

        protected async Task<IEnumerable<T>> FilterAsync<T>(string url, string filterName, string tenantName = null) 
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(url, tenantName)}?filterName={filterName}");
            return await response.ToObjectAsync<IEnumerable<T>>();
        }

        protected async Task<T> GetAsync<T>(string url, string name, string tenantName = null)
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(url, tenantName)}?name={name}");
            return await response.ToObjectAsync<T>();
        }

        protected async Task CreateAsync<T>(string url, T data, string tenantName = null)
        {
            using var response = await httpClient.PostAsFormatJsonAsync(await GetTenantApiUrlAsync(url, tenantName), data);
        }

        protected async Task UpdateAsync<T>(string url, T data, string tenantName = null)
        {
            using var response = await httpClient.PutAsFormatJsonAsync(await GetTenantApiUrlAsync(url, tenantName), data);
        }

        protected async Task DeleteAsync(string url, string name, string tenantName = null)
        {
            await httpClient.DeleteAsync($"{await GetTenantApiUrlAsync(url, tenantName)}?name={name}");
        }
    }
}
