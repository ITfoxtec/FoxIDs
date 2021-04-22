using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Azure.Core;
using System.Threading;
using System.Net.Http.Headers;
using FoxIDs.Models.Config;
using Microsoft.Azure.ApplicationInsights.Query.Models;

namespace FoxIDs.Controllers
{
    public class TTrackLogController : TenantApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly TokenCredential tokenCredential;

        public TTrackLogController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, IHttpClientFactory httpClientFactory, TokenCredential tokenCredential) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.mapper = mapper;
            this.httpClientFactory = httpClientFactory;
            this.tokenCredential = tokenCredential;
        }

        /// <summary>
        /// Get track log settings.
        /// </summary>
        /// <returns>Log settings.</returns>
        [ProducesResponseType(typeof(Api.LogSettings), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LogResponse>> GetTrackLog(Api.LogRequest logRequest)
        {
            var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessToken());


            var applicationInsightsQuery = new ApplicationInsightsQuery { Query = "requests | limit 5" };

            using var response = await httpClient.PostAsFormatJsonAsync(ApplicationInsightsUrl, applicationInsightsQuery);
            var queryResults = await response.ToObjectAsync<QueryResults>();

            //queryResults.Results.

            return Ok(new Api.LogResponse());

            //try
            //{
            //    var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });
            //    if (mTrack.Logging != null && mTrack.Logging.ScopedLogger != null)
            //    {
            //        return Ok(mapper.Map<Api.LogSettings>(mTrack.Logging.ScopedLogger));
            //    }
            //    else
            //    {
            //        return NoContent();
            //    }
            //}
            //catch (CosmosDataException ex)
            //{
            //    if (ex.StatusCode == HttpStatusCode.NotFound)
            //    {
            //        logger.Warning(ex, $"NotFound, Get {nameof(Track)}.{nameof(Track.Logging)}.{nameof(Track.Logging.ScopedLogger)} by track name '{RouteBinding.TrackName}'.");
            //        return NotFound($"{nameof(Track)}.{nameof(Track.Logging)}.{nameof(Track.Logging.ScopedLogger)}", RouteBinding.TrackName);
            //    }
            //    throw;
            //}
        }

        private async Task<string> GetAccessToken()
        {
            var accessToken = await tokenCredential.GetTokenAsync(new TokenRequestContext(new[] { "https://api.applicationinsights.io/.default" }), HttpContext.RequestAborted);
            return accessToken.Token;
        }

        private string ApplicationInsightsUrl => $"https://api.applicationinsights.io/v1/apps/{settings.ApplicationInsights.AppId}/query";
    }
}
