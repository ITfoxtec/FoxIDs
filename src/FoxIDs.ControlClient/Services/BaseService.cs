using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace FoxIDs.Client.Services
{
    public abstract class BaseService
    {
        public const string HttpClientSecureLogicalName = "FoxIDs.ControlAPI.Secure";
        public const string HttpClientLogicalName = "FoxIDs.ControlAPI";
        protected readonly HttpClient httpClient;
        protected readonly RouteBindingLogic routeBindingLogic;
        private readonly TrackSelectedLogic trackSelectedLogic;

        public BaseService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic, bool sendAccessToken = true)
        {
            httpClient = httpClientFactory.CreateClient(sendAccessToken ? HttpClientSecureLogicalName : HttpClientLogicalName);
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

        protected string GetApiUrl(string url, string tenantName, string trackName)
        {
            url = url.Replace("{tenant}", tenantName);            
            return GetApiUrl(url, trackName);
        }

        protected string GetApiUrl(string url, string trackName)
        {
            if (!trackName.IsNullOrWhiteSpace() && url.Contains("{track}"))
            {
                url = url.Replace("{track}", trackName);
            }
            return url;
        }

        protected async Task<PaginationResponse<T>> GetListAsync<T>(string url, string parmValue1 = null, string parmValue2 = null, string parmValue3 = null, string parmValue4 = null, string parmName1 = "filterName", string parmName2 = null, string parmName3 = null, string parmName4 = null, string paginationToken = null)
        {
            var parms = new List<string>();
            if (!parmValue1.IsNullOrWhiteSpace())
            {
                parms.Add($"{parmName1}={HttpUtility.UrlEncode(parmValue1)}");
            }
            if (!parmValue2.IsNullOrWhiteSpace())
            {
                parms.Add($"{parmName2}={HttpUtility.UrlEncode(parmValue2)}");
            }
            if (!parmValue3.IsNullOrWhiteSpace())
            {
                parms.Add($"{parmName3}={HttpUtility.UrlEncode(parmValue3)}");
            }
            if (!parmValue4.IsNullOrWhiteSpace())
            {
                parms.Add($"{parmName4}={HttpUtility.UrlEncode(parmValue4)}");
            }
            if (!paginationToken.IsNullOrWhiteSpace())
            {
                parms.Add($"paginationToken={paginationToken}");
            }

            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(url)}?{string.Join('&', parms)}");
            return await response.ToObjectAsync<PaginationResponse<T>>();
        }

        protected async Task<T> GetAsync<T>(string url)
        {
            using var response = await httpClient.GetAsync(await GetTenantApiUrlAsync(url));
            return await response.ToObjectAsync<T>();
        }

        protected async Task<T> GetAsync<T>(string url, string parmValue1, string parmValue2 = null, string parmValue3 = null, string parmName1 = "name", string parmName2 = null, string parmName3 = null)
        {
            var parms = new List<string>();
            if (!parmValue1.IsNullOrWhiteSpace())
            {
                parms.Add($"{parmName1}={HttpUtility.UrlEncode(parmValue1)}");
            }
            if (!parmValue2.IsNullOrWhiteSpace())
            {
                parms.Add($"{parmName2}={HttpUtility.UrlEncode(parmValue2)}");
            }
            if (!parmValue3.IsNullOrWhiteSpace())
            {
                parms.Add($"{parmName3}={HttpUtility.UrlEncode(parmValue3)}");
            }
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(url)}?{string.Join('&', parms)}");
            return await response.ToObjectAsync<T>();
        }

        protected async Task<TResponse> GetAsync<TRequest, TResponse>(string url, TRequest request)
        {
            var requestItems = request.ToDictionary();
            var requestUrl = QueryHelpers.AddQueryString(await GetTenantApiUrlAsync(url), requestItems);
            using var response = await httpClient.GetAsync(requestUrl);
            return await response.ToObjectAsync<TResponse>();
        }

        protected async Task PostAsync<T>(string url, T data)
        {
            using var response = await httpClient.PostAsFoxIDsApiJsonAsync(await GetTenantApiUrlAsync(url), data);
        }

        protected async Task<TResponse> PostResponseAsync<T, TResponse>(string url, T data)
        {
            using var response = await httpClient.PostAsFoxIDsApiJsonAsync(await GetTenantApiUrlAsync(url), data);
            return await response.ToObjectAsync<TResponse>();
        }

        protected async Task PutAsync<T>(string url, T data)
        {
            using var response = await httpClient.PutAsFoxIDsApiJsonAsync(await GetTenantApiUrlAsync(url), data);
        }

        protected async Task<TResponse> PutResponseAsync<T, TResponse>(string url, T data)
        {
            using var response = await httpClient.PutAsFoxIDsApiJsonAsync(await GetTenantApiUrlAsync(url), data);
            return await response.ToObjectAsync<TResponse>();
        }

        protected async Task DeleteAsync(string url)
        {
            using var response = await httpClient.DeleteAsync(await GetTenantApiUrlAsync(url));
        }

        protected async Task DeleteAsync(string url, string parmValue1, string parmValue2 = null, string parmValue3 = null, string parmName1 = "name", string parmName2 = null, string parmName3 = null)
        {
            var parms = new List<string>();
            if (!parmValue1.IsNullOrWhiteSpace())
            {
                parms.Add($"{parmName1}={HttpUtility.UrlEncode(parmValue1)}");
            }
            if (!parmValue2.IsNullOrWhiteSpace())
            {
                parms.Add($"{parmName2}={HttpUtility.UrlEncode(parmValue2)}");
            }
            if (!parmValue3.IsNullOrWhiteSpace())
            {
                parms.Add($"{parmName3}={HttpUtility.UrlEncode(parmValue3)}");
            }
            using var response = await httpClient.DeleteAsync($"{await GetTenantApiUrlAsync(url)}?{string.Join('&', parms)}");
        }

        protected async Task DeleteByRequestObjAsync<TRequest>(string url, TRequest request)
        {
            var requestItems = request.ToDictionary();
            var requestUrl = QueryHelpers.AddQueryString(await GetTenantApiUrlAsync(url), requestItems);
            using var response = await httpClient.DeleteAsync(requestUrl);
        }
    }
}
