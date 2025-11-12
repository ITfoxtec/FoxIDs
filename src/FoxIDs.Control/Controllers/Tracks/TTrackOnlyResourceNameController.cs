using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;
using FoxIDs.Logic;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TTrackOnlyResourceNameController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly TrackCacheLogic trackCacheLogic;

        public TTrackOnlyResourceNameController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, TrackCacheLogic trackCacheLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.trackCacheLogic = trackCacheLogic;
        }

        /// <summary>
        /// Update environment only resource name.
        /// </summary>
        /// <param name="trackResourceName">Resource name.</param>
        /// <returns>Resource item.</returns>
        [ProducesResponseType(typeof(Api.TrackResourceName), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.ResourceName>> PutTrackOnlyResourceName([FromBody] Api.TrackResourceName trackResourceName)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(trackResourceName)) return BadRequest(ModelState);

                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(trackIdKey);

                try
                {
                    var duplicatedName = mTrack.ResourceEnvelope?.Names?.FirstOrDefault(n => (trackResourceName.Id <= 0 || n.Id != trackResourceName.Id) && n.Name == trackResourceName.Name);
                    if (duplicatedName != null)
                    {
                        throw new ValidationException($"Duplicated name '{duplicatedName.Name}'.");
                    }
                }
                catch (ValidationException vex)
                {
                    logger.Warning(vex);
                    ModelState.TryAddModelError($"{nameof(trackResourceName.Name)}", vex.Message);
                    return BadRequest(ModelState);
                }

                if (mTrack.ResourceEnvelope == null)
                {
                    mTrack.ResourceEnvelope = new TrackResourceEnvelope();
                }
                if (mTrack.ResourceEnvelope.Names == null)
                {
                    mTrack.ResourceEnvelope.Names = new List<ResourceName>();
                }
                if (mTrack.ResourceEnvelope.Resources == null)
                {
                    mTrack.ResourceEnvelope.Resources = new List<ResourceItem>();
                }

                ResourceName resourceName = null;
                if (trackResourceName.Id > 0)
                {
                    resourceName = mTrack.ResourceEnvelope.Names.First(r => r.Id == trackResourceName.Id);
                    resourceName.Name = trackResourceName.Name;
                }
                else
                {
                    var currentNumbers = mTrack.ResourceEnvelope.Names.Select(r => r.Id);
                    if (currentNumbers.Count() <= 0)
                    {
                        currentNumbers = [0];
                    }
                    var nextNumber = Enumerable.Range(1, currentNumbers.Max())
                             .Except(currentNumbers)
                             .DefaultIfEmpty(currentNumbers.Max() + 1)
                             .Min();

                    resourceName = new ResourceName { Id = nextNumber, Name = trackResourceName.Name };
                    mTrack.ResourceEnvelope.Names.Add(resourceName);
                    var resourceitem = new ResourceItem { Id = nextNumber, Items = [new ResourceCultureItem { Culture = "en", Value = trackResourceName.Name }] };
                    mTrack.ResourceEnvelope.Resources.Add(resourceitem);
                }

                await tenantDataRepository.UpdateAsync(mTrack);
                await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

                return Ok(mapper.Map<Api.ResourceName>(resourceName));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.TrackResourceName).Name}' by environment name '{RouteBinding.TrackName}' and resource name '{trackResourceName.Name}'.");
                    return NotFound("Track.ResourceEnvelope.Names", trackResourceName.Name, nameof(Api.TrackResourceName.Id));
                }
                throw;
            }
        }

        /// <summary>
        /// Delete environment only resource name.
        /// </summary>
        /// <param name="name">Resource name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTrackOnlyResourceName(string name)
        {
            try
            {
                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(trackIdKey);

                if (mTrack.ResourceEnvelope?.Names?.Count > 0)
                {
                    if (mTrack.ResourceEnvelope?.Resources?.Count > 0)
                    {
                        var resourceName = mTrack.ResourceEnvelope.Names.FirstOrDefault(n => n.Name == name);
                        if (resourceName != null)
                        {
                            mTrack.ResourceEnvelope.Resources.RemoveAll(r => r.Id == resourceName.Id);
                        }
                    }

                    mTrack.ResourceEnvelope.Names.RemoveAll(n => n.Name == name);
                }

                await tenantDataRepository.UpdateAsync(mTrack);
                await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete environment only Resource name by environment name '{RouteBinding.TrackName}' and resource name '{name}'.");
                    return NotFound("Track.ResourceEnvelope.Names", name);
                }
                throw;
            }
        }
    }
}
