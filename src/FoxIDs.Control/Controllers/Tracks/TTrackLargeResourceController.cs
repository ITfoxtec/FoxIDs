using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Api = FoxIDs.Models.Api;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TTrackLargeResourceController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TTrackLargeResourceController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Get a large resource.
        /// </summary>
        /// <param name="name">Large resource name.</param>
        /// <returns>Large resource.</returns>
        [ProducesResponseType(typeof(Api.TrackLargeResourceItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackLargeResourceItem>> GetTrackLargeResource(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);
                name = name?.ToLower();

                var mTrackResourceLarge = await tenantDataRepository.GetAsync<TrackLargeResource>(await TrackLargeResource.IdFormatAsync(RouteBinding, name));

                return Ok(OrderCultureItems(mapper.Map<Api.TrackLargeResourceItem>(mTrackResourceLarge)));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.TrackLargeResourceItem).Name}' by environment name '{RouteBinding.TrackName}' and name '{name}'.");
                    return NotFound(typeof(Api.TrackLargeResourceItem).Name, name);
                }
                throw;
            }
        }

        /// <summary>
        /// Create a large resource.
        /// </summary>
        /// <param name="trackResourceLarge">Large resource to create.</param>
        /// <returns>Created large resource.</returns>
        [ProducesResponseType(typeof(Api.TrackLargeResourceItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.TrackLargeResourceItem>> PostTrackLargeResource([FromBody] Api.TrackLargeResourceItem trackResourceLarge)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(trackResourceLarge)) return BadRequest(ModelState);
                trackResourceLarge.Items.ForEach(item => item.Culture = item.Culture.ToLower());
                try
                {
                    ValidateCultures(trackResourceLarge);
                }
                catch (ValidationException vex)
                {
                    logger.Warning(vex);
                    ModelState.TryAddModelError($"{nameof(trackResourceLarge.Items)}.{nameof(Api.TrackLargeResourceCultureItem.Culture)}".ToCamelCase(), vex.Message);
                    return BadRequest(ModelState);
                }

                trackResourceLarge.Name = trackResourceLarge.Name.Trim();

                var mTrackResourceLarge = mapper.Map<TrackLargeResource>(trackResourceLarge);
                mTrackResourceLarge.Id = await TrackLargeResource.IdFormatAsync(RouteBinding, trackResourceLarge.Name);
                AddDefaultEnTranslation(mTrackResourceLarge);
                await tenantDataRepository.CreateAsync(mTrackResourceLarge);

                return Ok(OrderCultureItems(mapper.Map<Api.TrackLargeResourceItem>(mTrackResourceLarge)));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Create '{typeof(Api.TrackLargeResourceItem).Name}' by environment name '{RouteBinding.TrackName}' and name '{trackResourceLarge.Name}'.");
                    return NotFound(typeof(Api.TrackLargeResourceItem).Name, trackResourceLarge.Name);
                }
                throw;
            }
        }

        /// <summary>
        /// Update a large resource.
        /// </summary>
        /// <param name="trackResourceLarge">Large resource to update.</param>
        /// <returns>Updated large resource.</returns>
        [ProducesResponseType(typeof(Api.TrackLargeResourceItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.TrackLargeResourceItem>> PutTrackLargeResource([FromBody] Api.TrackLargeResourceItem trackResourceLarge)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(trackResourceLarge)) return BadRequest(ModelState);
                trackResourceLarge.Items.ForEach(item => item.Culture = item.Culture.ToLower());
                try
                {
                    ValidateCultures(trackResourceLarge);
                }
                catch (ValidationException vex)
                {
                    logger.Warning(vex);
                    ModelState.TryAddModelError($"{nameof(trackResourceLarge.Items)}.{nameof(Api.TrackLargeResourceCultureItem.Culture)}".ToCamelCase(), vex.Message);
                    return BadRequest(ModelState);
                }

                trackResourceLarge.Name = trackResourceLarge.Name?.Trim();

                var mTrackResourceLarge = await tenantDataRepository.GetAsync<TrackLargeResource>(await TrackLargeResource.IdFormatAsync(RouteBinding, trackResourceLarge.Name));
               
                mTrackResourceLarge.Items = mapper.Map<List<TrackLargeResourceCultureItem>>(trackResourceLarge.Items);
                AddDefaultEnTranslation(mTrackResourceLarge);
                await tenantDataRepository.UpdateAsync(mTrackResourceLarge);

                return Ok(OrderCultureItems(mapper.Map<Api.TrackLargeResourceItem>(mTrackResourceLarge)));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.TrackLargeResourceItem).Name}' by environment name '{RouteBinding.TrackName}' and name '{trackResourceLarge.Name}'.");
                    return NotFound(typeof(Api.TrackLargeResourceItem).Name, trackResourceLarge.Name);
                }
                throw;
            }
        }

        /// <summary>
        /// Delete a large resource.
        /// </summary>
        /// <param name="name">Large resource name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteTrackLargeResource(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);
                name = name?.ToLower();

                await tenantDataRepository.DeleteAsync<TrackLargeResource>(await TrackLargeResource.IdFormatAsync(RouteBinding, name));
                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.TrackLargeResourceItem).Name}' by environment name '{RouteBinding.TrackName}' and name '{name}'.");
                    return NotFound(typeof(Api.TrackLargeResourceItem).Name, name);
                }
                throw;
            }
        }

        private Api.TrackLargeResourceItem OrderCultureItems(Api.TrackLargeResourceItem resource)
        {
            if (resource?.Items != null)
            {
                resource.Items = resource.Items.OrderBy(i => i.Culture).ToList();
            }

            return resource;
        }

        private void ValidateCultures(Api.TrackLargeResourceItem resource)
        {
            if (resource?.Items?.Count > 0)
            {
                var duplicatedCulture = resource.Items.GroupBy(i => i.Culture).FirstOrDefault(g => g.Count() > 1);
                if (duplicatedCulture != null)
                {
                    throw new ValidationException($"Duplicated culture '{duplicatedCulture.Key}'.");
                }
            }
        }

        private void AddDefaultEnTranslation(TrackLargeResource mTrackResourceLarge)
        {
            var enItem = mTrackResourceLarge.Items.FirstOrDefault(i => i.Culture == Constants.Models.Resource.DefaultLanguage);

            if (enItem == null)
            {
                enItem = new TrackLargeResourceCultureItem
                {
                    Culture = Constants.Models.Resource.DefaultLanguage
                };
                mTrackResourceLarge.Items.Add(enItem);
            }

            if (enItem.Value.IsNullOrWhiteSpace())
            {
                enItem.Value = $"Add the default English text for '{mTrackResourceLarge.Name}'.";
            }
        }
    }
}
