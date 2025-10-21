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
    [TenantScopeAuthorize]
    [Obsolete($"Use {nameof(TTracksController)} instead.")]
    public class TFilterTrackController : ApiController
    {
        private const string dataType = Constants.Models.DataType.Track;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TFilterTrackController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Obsolete please use 'Tracks' instead.
        /// Filter track.
        /// </summary>
        /// <param name="filterName">Filter environment name.</param>
        /// <returns>Tracks.</returns>
        [ProducesResponseType(typeof(HashSet<Api.Track>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Obsolete($"Use {nameof(TTracksController)} instead.")]
    public async Task<ActionResult<HashSet<Api.Track>>> GetFilterTrack(string filterName)
        {
            try
            {
                filterName = filterName?.Trim();
                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                (var mTracks, _) = filterName.IsNullOrWhiteSpace() ? 
                    await tenantDataRepository.GetManyAsync<Track>(idKey, whereQuery: p => p.DataType.Equals(dataType)) : 
                    await tenantDataRepository.GetManyAsync<Track>(idKey, whereQuery: p => p.DataType.Equals(dataType) && 
                        (p.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase) || p.DisplayName.Contains(filterName, StringComparison.CurrentCultureIgnoreCase)));
               
                var aTracks = new HashSet<Api.Track>(mTracks.Count());
                foreach(var mTrack in mTracks.OrderBy(t => t.Name))
                {
                    aTracks.Add(mapper.Map<Api.Track>(mTrack));
                }
                return Ok(aTracks);
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
    }
}
