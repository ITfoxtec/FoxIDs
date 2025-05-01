using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using ITfoxtec.Identity;
using System;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.AnyTrack)]
    public class TTracksController : ApiController
    {
        private const string dataType = Constants.Models.DataType.Track;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TTracksController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Get environments.
        /// </summary>
        /// <param name="filterName">Filter environment name.</param>
        /// <param name="paginationToken">The pagination token.</param>
        /// <returns>Environments.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.Track>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.PaginationResponse<Api.Track>>> GetTracks(string filterName, string paginationToken = null)
        {
            try
            {
                (var mTracks, var nextPaginationToken) = await GetFilterTrackInternalAsync(filterName, paginationToken);

                var response = new Api.PaginationResponse<Api.Track>
                {
                    Data = new HashSet<Api.Track>(mTracks.Count()),
                    PaginationToken = nextPaginationToken,
                };                
                foreach(var mTrack in mTracks.OrderBy(t => t.Name))
                {
                    response.Data.Add(mapper.Map<Api.Track>(mTrack));
                }
                return Ok(response);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.Track).Name}' by filter name '{filterName}'.");
                    return NotFound(typeof(Api.Track).Name, filterName);
                }
                throw;
            }
        }

        private async Task<(IReadOnlyCollection<Track> mTracks, string nextPaginationToken)> GetFilterTrackInternalAsync(string filterName, string paginationToken)
        {
            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName };

            if (HttpContext.GetTenantScopeAccessToAnyTrack())
            {
                return filterName.IsNullOrWhiteSpace() ?
                    await tenantDataRepository.GetListAsync<Track>(idKey, whereQuery: p => p.DataType.Equals(dataType), paginationToken: paginationToken) :
                    await tenantDataRepository.GetListAsync<Track>(idKey, whereQuery: p => p.DataType.Equals(dataType) &&
                        (p.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase) || p.DisplayName.Contains(filterName, StringComparison.CurrentCultureIgnoreCase)), paginationToken: paginationToken);
            }

            var accessToTrackNames = HttpContext.GetTenantScopeAccessToTrackNames();
            if (accessToTrackNames?.Count() > 0)
            {
                return filterName.IsNullOrWhiteSpace() ?
                    await tenantDataRepository.GetListAsync<Track>(idKey, whereQuery: p => p.DataType.Equals(dataType) && accessToTrackNames.Any(at => at == p.Name), paginationToken: paginationToken) :
                    await tenantDataRepository.GetListAsync<Track>(idKey, whereQuery: p => p.DataType.Equals(dataType) && accessToTrackNames.Any(at => at == p.Name) &&
                        (p.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase) || p.DisplayName.Contains(filterName, StringComparison.CurrentCultureIgnoreCase)), paginationToken: paginationToken);

            }

            return (new List<Track>(), null);
        }
    }
}
