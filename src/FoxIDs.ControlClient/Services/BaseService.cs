using FoxIDs.Client.Logic;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public abstract class BaseService
    {
        public const string HttpClientSecureLogicalName = "FoxIDs.ControlAPI.Secure";
        public const string HttpClientLogicalName = "FoxIDs.ControlAPI";
        protected readonly HttpClient httpClient;
        protected readonly RouteBindingLogic routeBindingLogic;
        private readonly TrackSelectedLogic trackSelectedLogic;

        public BaseService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic, bool secureService = true)
        {
            httpClient = httpClientFactory.CreateClient(secureService ? HttpClientSecureLogicalName : HttpClientLogicalName);
            this.routeBindingLogic = routeBindingLogic;
            this.trackSelectedLogic = trackSelectedLogic;
        }

        protected async Task<string> GetTenantApiUrlAsync(string url)
        {
            var tenantName = await routeBindingLogic.GetTenantNameAsync();
            url = url.Replace("{tenant}", tenantName);
            if (url.Contains("{track}"))
            {
                if (!trackSelectedLogic.IsTrackSelected)
                {
                    throw new Exception("Track not selected.");
                }
                var trackName = trackSelectedLogic.Track.Name;
                url = url.Replace("{track}", trackName);
            }
            return url;
        }

        protected async Task<IEnumerable<T>> FilterAsync<T>(string url, string filterName, string parmName = "filterName") 
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(url)}?{parmName}={filterName}");
            return await response.ToObjectAsync<IEnumerable<T>>();
        }

        protected async Task<T> GetAsync<T>(string url)
        {
            using var response = await httpClient.GetAsync(await GetTenantApiUrlAsync(url));
            return await response.ToObjectAsync<T>();
        }

        protected async Task<T> GetAsync<T>(string url, string value, string parmName = "name")
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(url)}?{parmName}={value}");
            return await response.ToObjectAsync<T>();
        }

        protected async Task<TResponse> GetAsync<TRequest, TResponse>(string url, TRequest request)
        {
            var requestItems = request.ToDictionary();
            var requestUrl = QueryHelpers.AddQueryString(await GetTenantApiUrlAsync(url), requestItems);
            using var response = await httpClient.GetAsync(requestUrl);
            return await response.ToObjectAsync<TResponse>();
        }

        protected async Task<T> GetAsync<T>(string url, string value1, string value2, string parmName1, string parmName2)
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(url)}?{parmName1}={value1}&{parmName2}={value2}");
            return await response.ToObjectAsync<T>();
        }

        protected async Task PostAsync<T>(string url, T data)
        {
            using var response = await httpClient.PostAsFormatJsonAsync(await GetTenantApiUrlAsync(url), data);
        }

        protected async Task<TResponse> PostResponseAsync<T, TResponse>(string url, T data)
        {
            using var response = await httpClient.PostAsFormatJsonAsync(await GetTenantApiUrlAsync(url), data);
            return await response.ToObjectAsync<TResponse>();
        }

        protected async Task PutAsync<T>(string url, T data)
        {
            using var response = await httpClient.PutAsFormatJsonAsync(await GetTenantApiUrlAsync(url), data);
        }

        protected async Task<TResponse> PutResponseAsync<T, TResponse>(string url, T data)
        {
            using var response = await httpClient.PutAsFormatJsonAsync(await GetTenantApiUrlAsync(url), data);
            return await response.ToObjectAsync<TResponse>();
        }

        protected async Task DeleteAsync(string url)
        {
            using var response = await httpClient.DeleteAsync(await GetTenantApiUrlAsync(url));
        }

        protected async Task DeleteAsync(string url, string value, string parmName = "name")
        {
            using var response = await httpClient.DeleteAsync($"{await GetTenantApiUrlAsync(url)}?{parmName}={value}");
        }

        protected async Task DeleteAsync(string url, string value1, string value2, string parmName1, string parmName2)
        {
            using var response = await httpClient.DeleteAsync($"{await GetTenantApiUrlAsync(url)}?{parmName1}={value1}&{parmName2}={value2}");
        }

    }
}
