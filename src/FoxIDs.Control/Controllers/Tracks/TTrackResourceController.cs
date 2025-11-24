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
using ITfoxtec.Identity;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TTrackResourceController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly EmbeddedResourceLogic embeddedResourceLogic;
        private readonly TrackCacheLogic trackCacheLogic;

        public TTrackResourceController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, EmbeddedResourceLogic embeddedResourceLogic, TrackCacheLogic trackCacheLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.embeddedResourceLogic = embeddedResourceLogic;
            this.trackCacheLogic = trackCacheLogic;
        }

        /// <summary>
        /// Get environment resource.
        /// </summary>
        /// <param name="resourceId">Resource id.</param>
        /// <returns>Resource item.</returns>
        [ProducesResponseType(typeof(Api.ResourceItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.ResourceItem>> GetTrackResource(int resourceId)
        {
            try
            {
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });

                var mResourceItem = mTrack.Resources?.SingleOrDefault(r => r.Id == resourceId);

                return Ok(AddDefaultValues(resourceId, mapper.Map<Api.ResourceItem>(mResourceItem ?? new ResourceItem { Id = resourceId, Items = new List<ResourceCultureItem>() })));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get Track.Resource by environment name '{RouteBinding.TrackName}' and resource id '{resourceId}'.");
                    return NotFound("Track.Resource", Convert.ToString(resourceId));
                }
                throw;
            }
        }

        /// <summary>
        /// Update environment resource.
        /// </summary>
        /// <param name="trackResourceItem">Resource item.</param>
        /// <returns>Resource item.</returns>
        [ProducesResponseType(typeof(Api.TrackResourceItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackResourceItem>> PutTrackResource([FromBody] Api.TrackResourceItem trackResourceItem)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(trackResourceItem)) return BadRequest(ModelState);
                trackResourceItem.Items.ForEach(item => item.Culture = item.Culture.ToLower());
                try
                {
                    var duplicatedCulture = trackResourceItem.Items.GroupBy(i => i.Culture).Where(g => g.Count() > 1).FirstOrDefault();
                    if (duplicatedCulture != null)
                    {
                        throw new ValidationException($"Duplicated culture '{duplicatedCulture.Key}'.");
                    }
                }
                catch (ValidationException vex)
                {
                    logger.Warning(vex);
                    ModelState.TryAddModelError($"{nameof(trackResourceItem.Items)}.{nameof(ResourceCultureItem.Culture)}".ToCamelCase(), vex.Message);
                    return BadRequest(ModelState);
                }

                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(trackIdKey);

                if (mTrack.Resources == null)
                {
                    mTrack.Resources = new List<ResourceItem>();
                }

                var mResourceItem = mapper.Map<ResourceItem>(trackResourceItem);
                mResourceItem.Items = mResourceItem.Items.Where(i => !i.Value.IsNullOrWhiteSpace()).ToList();

                var itemIndex = mTrack.Resources.FindIndex(r => r.Id == trackResourceItem.Id);
                if (mResourceItem.Items.Count() > 0)
                {
                    if (itemIndex > -1)
                    {
                        mTrack.Resources[itemIndex] = mResourceItem;
                    }
                    else
                    {
                        mTrack.Resources.Add(mResourceItem);
                    }
                }
                else if (itemIndex > -1)
                {
                    mTrack.Resources.RemoveAt(itemIndex);
                }
                await tenantDataRepository.UpdateAsync(mTrack);

                await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

                return Ok(AddDefaultValues(trackResourceItem.Id, mapper.Map<Api.TrackResourceItem>(mResourceItem)));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.TrackResourceItem).Name}' by environment name '{RouteBinding.TrackName}' and resource id '{trackResourceItem.Id}'.");
                    return NotFound(typeof(Api.TrackResourceItem).Name, Convert.ToString(trackResourceItem.Id), nameof(trackResourceItem.Id));
                }
                throw;
            }
        }

        /// <summary>
        /// Delete environment resource.
        /// </summary>
        /// <param name="resourceId">Resource id.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTrackResource(int resourceId)
        {
            try
            {
                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(trackIdKey);
                if(mTrack.Resources?.Count > 0)
                {
                    var removed = mTrack.Resources.RemoveAll(r => r.Id == resourceId);
                    if (removed > 0)
                    {
                        await tenantDataRepository.UpdateAsync(mTrack);
                        await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);
                    }
                }

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete Track.Resource by environment name '{RouteBinding.TrackName}' and resource id '{resourceId}'.");
                    return NotFound("Track.Resource", Convert.ToString(resourceId));
                }
                throw;
            }
        }

        private Api.ResourceItem AddDefaultValues(int resourceId, Api.ResourceItem resourceItem)
        {
            var embeddedResourceEnvelope = embeddedResourceLogic.GetResourceEnvelope();
            var embeddedResourceItem = embeddedResourceEnvelope.Resources.SingleOrDefault(r => r.Id == resourceId);
            if (embeddedResourceItem == null)
            {
                throw new FoxIDsDataException("Embedded resource do not exist.");
            }

            foreach (var embeddedItem in embeddedResourceItem.Items)
            {
                var item = resourceItem.Items.SingleOrDefault(i => i.Culture == embeddedItem.Culture);
                if (item == null)
                {
                    item = new Api.ResourceCultureItem { Culture = embeddedItem.Culture };
                    resourceItem.Items.Add(item);
                }
                item.DefaultValue = embeddedItem.Value;
            }

            resourceItem.Items = resourceItem.Items.OrderBy(i => i.Culture).ToList();
            return resourceItem;
        }
    }
}
