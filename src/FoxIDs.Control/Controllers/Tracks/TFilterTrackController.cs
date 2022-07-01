using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using ITfoxtec.Identity;

namespace FoxIDs.Controllers
{
    public class TFilterTrackController : TenantApiController
    {
        private const string dataType = "track";
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;

        public TFilterTrackController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
        }

        /// <summary>
        /// Filter track.
        /// </summary>
        /// <param name="filterName">Filter track name.</param>
        /// <returns>Tracks.</returns>
        [ProducesResponseType(typeof(HashSet<Api.Track>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HashSet<Api.Track>>> GetFilterTrack(string filterName)
        {
            try
            {
                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTracks = filterName.IsNullOrWhiteSpace() ? await tenantRepository.GetListAsync<Track>(idKey, whereQuery: p => p.DataType.Equals(dataType)) : await tenantRepository.GetListAsync<Track>(idKey, whereQuery: p => p.DataType.Equals(dataType) && p.Name.Contains(filterName));
                var aTracks = new HashSet<Api.Track>(mTracks.Count());
                foreach(var mTrack in mTracks.OrderBy(t => t.Name))
                {
                    aTracks.Add(mapper.Map<Api.Track>(mTrack));
                }
                return Ok(aTracks);
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.Track).Name}' by filter name '{filterName}'.");
                    return NotFound(typeof(Api.Track).Name, filterName);
                }
                throw;
            }
        }
    }
}
