﻿using FoxIDs.Client.Logic;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public abstract class BaseService
    {
        protected readonly HttpClient httpClient;
        protected readonly RouteBindingLogic routeBindingLogic;
        private readonly TrackSelectedLogic trackSelectedLogic;

        public BaseService(HttpClient httpClient, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic)
        {
            this.httpClient = httpClient;
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
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(url)}");
            return await response.ToObjectAsync<T>();
        }

        protected async Task<T> GetAsync<T>(string url, string value, string parmName = "name")
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(url)}?{parmName}={value}");
            return await response.ToObjectAsync<T>();
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

        protected async Task PutAsync<T>(string url, T data)
        {
            using var response = await httpClient.PutAsFormatJsonAsync(await GetTenantApiUrlAsync(url), data);
        }

        protected async Task DeleteAsync(string url)
        {
            await httpClient.DeleteAsync($"{await GetTenantApiUrlAsync(url)}");
        }

        protected async Task DeleteAsync(string url, string value, string parmName = "name")
        {
            await httpClient.DeleteAsync($"{await GetTenantApiUrlAsync(url)}?{parmName}={value}");
        }

        protected async Task DeleteAsync(string url, string value1, string value2, string parmName1, string parmName2)
        {
            await httpClient.DeleteAsync($"{await GetTenantApiUrlAsync(url)}?{parmName1}={value1}&{parmName2}={value2}");
        }

    }
}
