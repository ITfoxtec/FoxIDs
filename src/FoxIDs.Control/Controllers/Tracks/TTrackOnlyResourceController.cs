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
    public class TTrackOnlyResourceController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly EmbeddedResourceLogic embeddedResourceLogic;
        private readonly TrackCacheLogic trackCacheLogic;

        public TTrackOnlyResourceController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, EmbeddedResourceLogic embeddedResourceLogic, TrackCacheLogic trackCacheLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.embeddedResourceLogic = embeddedResourceLogic;
            this.trackCacheLogic = trackCacheLogic;
        }

        /// <summary>
        /// Get environment only resource.
        /// </summary>
        /// <param name="resourceId">Resource id.</param>
        /// <returns>Resource item.</returns>
        [ProducesResponseType(typeof(Api.ResourceItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.ResourceItem>> GetTrackOnlyResource(int resourceId)
        {
            try
            {
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });

                var mResourceItem = mTrack.ResourceEnvelope?.Resources?.FirstOrDefault(r => r.Id == resourceId);

                return Ok(AddDefaultCultures(mapper.Map<Api.ResourceItem>(mResourceItem ?? new ResourceItem { Id = resourceId, Items = new List<ResourceCultureItem>() })));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get environment only Resource by environment name '{RouteBinding.TrackName}' and resource id '{resourceId}'.");
                    return NotFound("Track.ResourceEnvelope.Resources", Convert.ToString(resourceId));
                }
                throw;
            }
        }

        /// <summary>
        /// Update environment only resource.
        /// </summary>
        /// <param name="trackResourceItem">Resource item.</param>
        /// <returns>Resource item.</returns>
        [ProducesResponseType(typeof(Api.TrackResourceItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackResourceItem>> PutTrackOnlyResource([FromBody] Api.TrackResourceItem trackResourceItem)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(trackResourceItem)) return BadRequest(ModelState);
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

                var mResourceItem = mapper.Map<ResourceItem>(trackResourceItem);
                mResourceItem.Items = mResourceItem.Items.Where(i => !i.Value.IsNullOrWhiteSpace()).ToList();

                if (mTrack.ResourceEnvelope == null)
                {
                    mTrack.ResourceEnvelope = new TrackResourceEnvelope();
                }
                if (mTrack.ResourceEnvelope.Resources == null)
                {
                    mTrack.ResourceEnvelope.Resources = new List<ResourceItem>();
                }

                UpdateResource(trackResourceItem.Id, mTrack.ResourceEnvelope.Resources, mResourceItem);

                await tenantDataRepository.UpdateAsync(mTrack);
                await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

                return Ok(AddDefaultCultures(mapper.Map<Api.TrackResourceItem>(mResourceItem)));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update environment only Resource by environment name '{RouteBinding.TrackName}' and resource id '{trackResourceItem.Id}'.");
                    return NotFound("Track.ResourceEnvelope.Resources", Convert.ToString(trackResourceItem.Id), nameof(trackResourceItem.Id));
                }
                throw;
            }
        }

        private void UpdateResource(int resourceId, List<ResourceItem> mResourceItems, ResourceItem mNewResourceItem)
        {
            var itemIndex = mResourceItems.FindIndex(r => r.Id == resourceId);
            if (mNewResourceItem.Items.Count() > 0)
            {
                if (itemIndex > -1)
                {
                    mResourceItems[itemIndex] = mNewResourceItem;
                }
                else
                {
                    mResourceItems.Add(mNewResourceItem);
                }
            }
            else if (itemIndex > -1)
            {
                mResourceItems.RemoveAt(itemIndex);
            }
        }

        /// <summary>
        /// Delete environment only resource.
        /// </summary>
        /// <param name="resourceId">Resource id.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTrackOnlyResource(int resourceId)
        {
            try
            {
                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(trackIdKey);
                
                if (mTrack.ResourceEnvelope?.Resources?.Count > 0)
                {
                    var removed = mTrack.ResourceEnvelope.Resources.RemoveAll(r => r.Id == resourceId);
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
                    logger.Warning(ex, $"NotFound, Delete environment only Resource by environment name '{RouteBinding.TrackName}' and resource id '{resourceId}'.");
                    return NotFound("Track.ResourceEnvelope.Resources", Convert.ToString(resourceId));
                }
                throw;
            }
        }

        private Api.ResourceItem AddDefaultCultures(Api.ResourceItem resourceItem)
        {
            var embeddedResourceEnvelope = embeddedResourceLogic.GetResourceEnvelope();

            foreach (var embeddedCulture in embeddedResourceEnvelope.SupportedCultures)
            {
                var item = resourceItem.Items.SingleOrDefault(i => i.Culture == embeddedCulture);
                if (item == null)
                {
                    item = new Api.ResourceCultureItem { Culture = embeddedCulture };
                    resourceItem.Items.Add(item);
                }
            }

            resourceItem.Items = resourceItem.Items.OrderBy(i => i.Culture).ToList();
            return resourceItem;
        }
    }
}
