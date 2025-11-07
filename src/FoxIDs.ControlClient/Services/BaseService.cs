using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
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

        public BaseService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic, bool sendAccessToken = true)
        {
            httpClient = httpClientFactory.CreateClient(sendAccessToken ? HttpClientSecureLogicalName : HttpClientLogicalName);
            this.routeBindingLogic = routeBindingLogic;
            this.trackSelectedLogic = trackSelectedLogic;
        }

        protected async Task<string> GetTenantApiUrlAsync(string url)
        {
            var tenantName = await routeBindingLogic.GetTenantNameAsync().ConfigureAwait(false);
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

        protected async Task<PaginationResponse<T>> GetListAsync<T>(string url, string parmValue1 = null, string parmValue2 = null, string parmValue3 = null, string parmValue4 = null, string parmName1 = "filterName", string parmName2 = null, string parmName3 = null, string parmName4 = null, string paginationToken = null, CancellationToken cancellationToken = default)
        {
            var queryParameters = CreateQueryParameters();
            TryAddParameter(queryParameters, parmName1, parmValue1);
            TryAddParameter(queryParameters, parmName2, parmValue2);
            TryAddParameter(queryParameters, parmName3, parmValue3);
            TryAddParameter(queryParameters, parmName4, parmValue4);
            TryAddParameter(queryParameters, "paginationToken", paginationToken);

            var requestUrl = await BuildTenantRequestUrlAsync(url, queryParameters).ConfigureAwait(false);
            using var response = await httpClient.GetAsync(requestUrl, cancellationToken).ConfigureAwait(false);
            return await response.ToObjectAsync<PaginationResponse<T>>().ConfigureAwait(false);
        }

        protected async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken = default)
        {
            var requestUrl = await GetTenantApiUrlAsync(url).ConfigureAwait(false);
            using var response = await httpClient.GetAsync(requestUrl, cancellationToken).ConfigureAwait(false);
            return await response.ToObjectAsync<T>().ConfigureAwait(false);
        }

        protected async Task<T> GetAsync<T>(string url, string parmValue1, string parmValue2 = null, string parmValue3 = null, string parmName1 = "name", string parmName2 = null, string parmName3 = null, CancellationToken cancellationToken = default)
        {
            var queryParameters = CreateQueryParameters();
            TryAddParameter(queryParameters, parmName1, parmValue1);
            TryAddParameter(queryParameters, parmName2, parmValue2);
            TryAddParameter(queryParameters, parmName3, parmValue3);

            var requestUrl = await BuildTenantRequestUrlAsync(url, queryParameters).ConfigureAwait(false);
            using var response = await httpClient.GetAsync(requestUrl, cancellationToken).ConfigureAwait(false);
            return await response.ToObjectAsync<T>().ConfigureAwait(false);
        }

        protected async Task<TResponse> GetAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken cancellationToken = default)
        {
            var requestItems = request.ToDictionary();
            var requestUrl = QueryHelpers.AddQueryString(await GetTenantApiUrlAsync(url).ConfigureAwait(false), requestItems);
            using var response = await httpClient.GetAsync(requestUrl, cancellationToken).ConfigureAwait(false);
            return await response.ToObjectAsync<TResponse>().ConfigureAwait(false);
        }

        protected async Task PostAsync<T>(string url, T data, CancellationToken cancellationToken = default)
        {
            using var response = await httpClient.PostAsFoxIDsApiJsonAsync(await GetTenantApiUrlAsync(url).ConfigureAwait(false), data, cancellationToken).ConfigureAwait(false);
        }

        protected async Task<TResponse> PostResponseAsync<T, TResponse>(string url, T data, CancellationToken cancellationToken = default)
        {
            using var response = await httpClient.PostAsFoxIDsApiJsonAsync(await GetTenantApiUrlAsync(url).ConfigureAwait(false), data, cancellationToken).ConfigureAwait(false);
            return await response.ToObjectAsync<TResponse>().ConfigureAwait(false);
        }

        protected async Task PutAsync<T>(string url, T data, CancellationToken cancellationToken = default)
        {
            using var response = await httpClient.PutAsFoxIDsApiJsonAsync(await GetTenantApiUrlAsync(url).ConfigureAwait(false), data, cancellationToken).ConfigureAwait(false);
        }

        protected async Task<TResponse> PutResponseAsync<T, TResponse>(string url, T data, CancellationToken cancellationToken = default)
        {
            using var response = await httpClient.PutAsFoxIDsApiJsonAsync(await GetTenantApiUrlAsync(url).ConfigureAwait(false), data, cancellationToken).ConfigureAwait(false);
            return await response.ToObjectAsync<TResponse>().ConfigureAwait(false);
        }

        protected async Task DeleteAsync(string url, CancellationToken cancellationToken = default)
        {
            using var response = await httpClient.DeleteAsync(await GetTenantApiUrlAsync(url).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }

        protected async Task DeleteAsync(string url, string parmValue1, string parmValue2 = null, string parmValue3 = null, string parmName1 = "name", string parmName2 = null, string parmName3 = null, CancellationToken cancellationToken = default)
        {
            var queryParameters = CreateQueryParameters();
            TryAddParameter(queryParameters, parmName1, parmValue1);
            TryAddParameter(queryParameters, parmName2, parmValue2);
            TryAddParameter(queryParameters, parmName3, parmValue3);

            var requestUrl = await BuildTenantRequestUrlAsync(url, queryParameters).ConfigureAwait(false);
            using var response = await httpClient.DeleteAsync(requestUrl, cancellationToken).ConfigureAwait(false);
        }

        protected async Task DeleteByRequestObjAsync<TRequest>(string url, TRequest request, CancellationToken cancellationToken = default)
        {
            var requestItems = request.ToDictionary();
            var requestUrl = QueryHelpers.AddQueryString(await GetTenantApiUrlAsync(url).ConfigureAwait(false), requestItems);
            using var response = await httpClient.DeleteAsync(requestUrl, cancellationToken).ConfigureAwait(false);
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
            var tenantUrl = await GetTenantApiUrlAsync(url).ConfigureAwait(false);
            if (queryParameters == null || queryParameters.Count == 0)
            {
                return tenantUrl;
            }

            return QueryHelpers.AddQueryString(tenantUrl, queryParameters);
        }
    }
}
