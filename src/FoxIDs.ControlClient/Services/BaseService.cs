using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
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
        protected readonly IHttpClientFactory httpClientFactory;
        protected readonly RouteBindingLogic routeBindingLogic;
        private readonly TrackSelectedLogic trackSelectedLogic;
        private readonly bool sendAccessToken;

        public BaseService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic, bool sendAccessToken = true)
        {
            this.httpClientFactory = httpClientFactory;
            this.routeBindingLogic = routeBindingLogic;
            this.trackSelectedLogic = trackSelectedLogic;
            this.sendAccessToken = sendAccessToken;
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
            var queryParameters = CreateQueryParameters();
            TryAddParameter(queryParameters, parmName1, parmValue1);
            TryAddParameter(queryParameters, parmName2, parmValue2);
            TryAddParameter(queryParameters, parmName3, parmValue3);
            TryAddParameter(queryParameters, parmName4, parmValue4);
            TryAddParameter(queryParameters, "paginationToken", paginationToken);

            var requestUrl = await BuildTenantRequestUrlAsync(url, queryParameters);
            using var httpClient = GetHttpClient();
            using var response = await httpClient.GetAsync(requestUrl);
            return await response.ToObjectAsync<PaginationResponse<T>>();
        }

        protected async Task<T> GetAsync<T>(string url)
        {
            var requestUrl = await GetTenantApiUrlAsync(url);
            using var httpClient = GetHttpClient();
            using var response = await httpClient.GetAsync(requestUrl);
            return await response.ToObjectAsync<T>();
        }

        protected async Task<T> GetAsync<T>(string url, string parmValue1, string parmValue2 = null, string parmValue3 = null, string parmName1 = "name", string parmName2 = null, string parmName3 = null)
        {
            var queryParameters = CreateQueryParameters();
            TryAddParameter(queryParameters, parmName1, parmValue1);
            TryAddParameter(queryParameters, parmName2, parmValue2);
            TryAddParameter(queryParameters, parmName3, parmValue3);

            var requestUrl = await BuildTenantRequestUrlAsync(url, queryParameters);
            using var httpClient = GetHttpClient();
            using var response = await httpClient.GetAsync(requestUrl);
            return await response.ToObjectAsync<T>();
        }

        protected async Task<TResponse> GetAsync<TRequest, TResponse>(string url, TRequest request)
        {
            var requestItems = request.ToDictionary();
            var requestUrl = QueryHelpers.AddQueryString(await GetTenantApiUrlAsync(url), requestItems);
            using var httpClient = GetHttpClient();
            using var response = await httpClient.GetAsync(requestUrl);
            return await response.ToObjectAsync<TResponse>();
        }

        protected async Task PostAsync<T>(string url, T data)
        {
            using var httpClient = GetHttpClient();
            using var response = await httpClient.PostAsFoxIDsApiJsonAsync(await GetTenantApiUrlAsync(url), data);
        }

        protected async Task<TResponse> PostResponseAsync<T, TResponse>(string url, T data)
        {
            using var httpClient = GetHttpClient();
            using var response = await httpClient.PostAsFoxIDsApiJsonAsync(await GetTenantApiUrlAsync(url), data);
            return await response.ToObjectAsync<TResponse>();
        }

        protected async Task PutAsync<T>(string url, T data)
        {
            using var httpClient = GetHttpClient();
            using var response = await httpClient.PutAsFoxIDsApiJsonAsync(await GetTenantApiUrlAsync(url), data);
        }

        protected async Task<TResponse> PutResponseAsync<T, TResponse>(string url, T data)
        {
            using var httpClient = GetHttpClient();
            using var response = await httpClient.PutAsFoxIDsApiJsonAsync(await GetTenantApiUrlAsync(url), data);
            return await response.ToObjectAsync<TResponse>();
        }

        protected async Task DeleteAsync(string url)
        {
            using var httpClient = GetHttpClient();
            using var response = await httpClient.DeleteAsync(await GetTenantApiUrlAsync(url));
        }

        protected async Task DeleteAsync(string url, string parmValue1, string parmValue2 = null, string parmValue3 = null, string parmName1 = "name", string parmName2 = null, string parmName3 = null)
        {
            var queryParameters = CreateQueryParameters();
            TryAddParameter(queryParameters, parmName1, parmValue1);
            TryAddParameter(queryParameters, parmName2, parmValue2);
            TryAddParameter(queryParameters, parmName3, parmValue3);

            var requestUrl = await BuildTenantRequestUrlAsync(url, queryParameters);
            using var httpClient = GetHttpClient();
            using var response = await httpClient.DeleteAsync(requestUrl);
        }

        protected async Task DeleteAsync(string url, string parmValue1, string parmValue2, string parmValue3, string parmValue4, string parmName1 = "name", string parmName2 = null, string parmName3 = null, string parmName4 = null)
        {
            var queryParameters = CreateQueryParameters();
            TryAddParameter(queryParameters, parmName1, parmValue1);
            TryAddParameter(queryParameters, parmName2, parmValue2);
            TryAddParameter(queryParameters, parmName3, parmValue3);
            TryAddParameter(queryParameters, parmName4, parmValue4);

            var requestUrl = await BuildTenantRequestUrlAsync(url, queryParameters);
            using var httpClient = GetHttpClient();
            using var response = await httpClient.DeleteAsync(requestUrl);
        }

        protected async Task DeleteByRequestObjAsync<TRequest>(string url, TRequest request)
        {
            var requestItems = request.ToDictionary();
            var requestUrl = QueryHelpers.AddQueryString(await GetTenantApiUrlAsync(url), requestItems);
            using var httpClient = GetHttpClient();
            using var response = await httpClient.DeleteAsync(requestUrl);
        }

        private static Dictionary<string, string> CreateQueryParameters() => new(StringComparer.Ordinal);

        private static void TryAddParameter(IDictionary<string, string> queryParameters, string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(name) && !value.IsNullOrWhiteSpace())
            {
                queryParameters[name] = value;
            }
        }

        private async Task<string> BuildTenantRequestUrlAsync(string url, IDictionary<string, string> queryParameters)
        {
            var tenantUrl = await GetTenantApiUrlAsync(url);
            if (queryParameters == null || queryParameters.Count == 0)
            {
                return tenantUrl;
            }

            return QueryHelpers.AddQueryString(tenantUrl, queryParameters);
        }

        private HttpClient GetHttpClient()
        {
            return httpClientFactory.CreateClient(sendAccessToken ? HttpClientSecureLogicalName : HttpClientLogicalName);
        }
    }
}
