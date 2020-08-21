using FoxIDs.Client.Logic;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public abstract class BaseService
    {
        protected readonly HttpClient httpClient;
        protected readonly RouteBindingLogic routeBindingLogic;

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

        protected async Task<IEnumerable<T>> FilterAsync<T>(string url, string filterName, string parmName = "filterName", string tenantName = null) 
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(url, tenantName)}?{parmName}={filterName}");
            return await response.ToObjectAsync<IEnumerable<T>>();
        }

        protected async Task<T> GetAsync<T>(string url, string value, string parmName = "name", string tenantName = null)
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(url, tenantName)}?{parmName}={value}");
            return await response.ToObjectAsync<T>();
        }

        protected async Task PostAsync<T>(string url, T data, string tenantName = null)
        {
            using var response = await httpClient.PostAsFormatJsonAsync(await GetTenantApiUrlAsync(url, tenantName), data);
        }

        protected async Task PutAsync<T>(string url, T data, string tenantName = null)
        {
            using var response = await httpClient.PutAsFormatJsonAsync(await GetTenantApiUrlAsync(url, tenantName), data);
        }

        protected async Task DeleteAsync(string url, string value, string parmName = "name", string tenantName = null)
        {
            await httpClient.DeleteAsync($"{await GetTenantApiUrlAsync(url, tenantName)}?{parmName}={value}");
        }
    }
}
