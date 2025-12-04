using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using ITfoxtec.Identity;
using System;
using FoxIDs.Infrastructure.Security;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TTrackOnlyResourceNamesController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TTrackOnlyResourceNamesController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Get environment only resource names and IDs.
        /// </summary>
        /// <param name="filterName">Filter environment only resource name or ID.</param>
        /// <returns>Resource names.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.ResourceName>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.PaginationResponse<Api.ResourceName>>> GetTrackOnlyResourceNames(string filterName, string paginationToken = null)
        {

            try
            {
                filterName = filterName?.Trim();
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });

                var filderResourceNames = mTrack.ResourceEnvelope?.Names;
                if (!filterName.IsNullOrWhiteSpace() && mTrack.ResourceEnvelope != null)
                {
                    var filterIds = mTrack.ResourceEnvelope.Names.Where(r => r.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase)).Select(r => r.Id).ToList();
                    if (int.TryParse(filterName, out var filterId))
                    {
                        filterIds.Add(filterId);
                    }

                    var resourceIds = mTrack.ResourceEnvelope.Resources?
                        .Where(r => r.Items != null && r.Items.Any(i => i.Value.Contains(filterName, StringComparison.CurrentCultureIgnoreCase)))
                        .Select(r => r.Id) ?? Enumerable.Empty<int>();

                    var filteredIds = filterIds.Concat(resourceIds).ToHashSet();
                    filderResourceNames = mTrack.ResourceEnvelope.Names.Where(r => filteredIds.Contains(r.Id)).ToList();
                }

                var response = new Api.PaginationResponse<Api.ResourceName>
                {
                    Data = mapper.Map<HashSet<Api.ResourceName>>(filderResourceNames?.OrderBy(r => r.Id)),
                };

                return Ok(response);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get environment ResourceEnvelope.Names by environment name '{RouteBinding.TrackName}'.");
                    return NotFound("Track.ResourceEnvelope.Names", RouteBinding.TrackName);
                }
                throw;
            }
        }
    }
}
