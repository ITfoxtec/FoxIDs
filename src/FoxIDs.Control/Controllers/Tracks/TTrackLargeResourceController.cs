using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models;
using FoxIDs.Repository;
using FoxIDs.Util;
using Api = FoxIDs.Models.Api;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TTrackLargeResourceController : ApiController
    {
        private const string dataType = Constants.Models.DataType.TrackLargeResource;
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
        /// Get large resources defined on the environment.
        /// </summary>
        /// <param name="filterName">Filter by resource key.</param>
        /// <param name="paginationToken">Pagination token.</param>
        /// <returns>Large track resources.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.TrackLargeResourceItem>), StatusCodes.Status200OK)]
        public async Task<ActionResult<Api.PaginationResponse<Api.TrackLargeResourceItem>>> GetTrackLargeResources(string filterName, string paginationToken = null)
        {
            try
            {
                filterName = filterName?.Trim();
                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                Expression<Func<TrackLargeResource, bool>> whereQuery = r => r.DataType.Equals(dataType);
                if (!filterName.IsNullOrWhiteSpace())
                {
                    whereQuery = r => r.DataType.Equals(dataType) && r.Name.Contains(filterName, StringComparison.InvariantCultureIgnoreCase);
                }

                (var mResources, var nextPaginationToken) = await tenantDataRepository.GetManyAsync<TrackLargeResource>(trackIdKey, whereQuery: whereQuery, paginationToken: paginationToken);

                var response = new Api.PaginationResponse<Api.TrackLargeResourceItem>
                {
                    Data = new HashSet<Api.TrackLargeResourceItem>(mResources.Count()),
                    PaginationToken = nextPaginationToken
                };

                foreach (var mResource in mResources.OrderBy(r => r.Name))
                {
                    var apiResource = mapper.Map<Api.TrackLargeResourceItem>(mResource);
                    OrderCultureItems(apiResource);
                    response.Data.Add(apiResource);
                }

                return Ok(response);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.TrackLargeResourceItem).Name}' by environment name '{RouteBinding.TrackName}'.");
                    return NotFound(typeof(Api.TrackLargeResourceItem).Name, RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Get a large resource.
        /// </summary>
        /// <param name="resourceId">Resource identifier.</param>
        /// <returns>Large resource.</returns>
        [ProducesResponseType(typeof(Api.TrackLargeResourceItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackLargeResourceItem>> GetTrackLargeResource(string resourceId)
        {
            try
            {
                var mResource = await tenantDataRepository.GetAsync<TrackLargeResource>(resourceId, required: false);
                if (mResource == null || !mResource.DataType.Equals(dataType, StringComparison.Ordinal))
                {
                    return NotFound(typeof(Api.TrackLargeResourceItem).Name, resourceId);
                }

                var apiResource = mapper.Map<Api.TrackLargeResourceItem>(mResource);
                OrderCultureItems(apiResource);
                return Ok(apiResource);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.TrackLargeResourceItem).Name}' by environment name '{RouteBinding.TrackName}' and id '{resourceId}'.");
                    return NotFound(typeof(Api.TrackLargeResourceItem).Name, resourceId);
                }
                throw;
            }
        }

        /// <summary>
        /// Create a large resource.
        /// </summary>
        /// <param name="trackResourceLargeItem">Resource to create.</param>
        /// <returns>Created resource.</returns>
        [ProducesResponseType(typeof(Api.TrackLargeResourceItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.TrackLargeResourceItem>> PostTrackLargeResource([FromBody] Api.TrackLargeResourceItem trackResourceLargeItem)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(trackResourceLargeItem)) return BadRequest(ModelState);
                try
                {
                    ValidateCultures(trackResourceLargeItem);
                }
                catch (ValidationException vex)
                {
                    logger.Warning(vex);
                    ModelState.TryAddModelError($"{nameof(trackResourceLargeItem.Items)}.{nameof(Api.TrackLargeResourceCultureItem.Culture)}".ToCamelCase(), vex.Message);
                    return BadRequest(ModelState);
                }

                trackResourceLargeItem.Name = trackResourceLargeItem.Name?.Trim();
                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                if (await NameExistsAsync(trackIdKey, trackResourceLargeItem.Name))
                {
                    return Conflict(nameof(Api.TrackLargeResourceItem), trackResourceLargeItem.Name, nameof(trackResourceLargeItem.Name));
                }

                var idKey = new TrackLargeResource.IdKey
                {
                    TenantName = trackIdKey.TenantName,
                    TrackName = trackIdKey.TrackName,
                    UniqueId = RandomName.GenerateDefaultName()
                };

                var mResource = mapper.Map<TrackLargeResource>(trackResourceLargeItem);
                mResource.Id = await TrackLargeResource.IdFormatAsync(idKey);
                NormalizeItems(mResource);

                await tenantDataRepository.CreateAsync(mResource);

                var apiResource = mapper.Map<Api.TrackLargeResourceItem>(mResource);
                OrderCultureItems(apiResource);
                return Ok(apiResource);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Create '{typeof(Api.TrackLargeResourceItem).Name}' by environment name '{RouteBinding.TrackName}'.");
                    return NotFound(typeof(Api.TrackLargeResourceItem).Name, RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Update a large resource.
        /// </summary>
        /// <param name="trackResourceLargeItem">Resource to update.</param>
        /// <returns>Updated resource.</returns>
        [ProducesResponseType(typeof(Api.TrackLargeResourceItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.TrackLargeResourceItem>> PutTrackLargeResource([FromBody] Api.TrackLargeResourceItem trackResourceLargeItem)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(trackResourceLargeItem)) return BadRequest(ModelState);
                try
                {
                    ValidateCultures(trackResourceLargeItem);
                }
                catch (ValidationException vex)
                {
                    logger.Warning(vex);
                    ModelState.TryAddModelError($"{nameof(trackResourceLargeItem.Items)}.{nameof(Api.TrackLargeResourceCultureItem.Culture)}".ToCamelCase(), vex.Message);
                    return BadRequest(ModelState);
                }

                trackResourceLargeItem.Name = trackResourceLargeItem.Name?.Trim();
                if (trackResourceLargeItem.Id.IsNullOrWhiteSpace())
                {
                    ModelState.TryAddModelError(nameof(trackResourceLargeItem.Id), "Resource id is required.");
                    return BadRequest(ModelState);
                }

                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                if (await NameExistsAsync(trackIdKey, trackResourceLargeItem.Name, trackResourceLargeItem.Id))
                {
                    return Conflict(nameof(Api.TrackLargeResourceItem), trackResourceLargeItem.Name, nameof(trackResourceLargeItem.Name));
                }

                var existing = await tenantDataRepository.GetAsync<TrackLargeResource>(trackResourceLargeItem.Id, required: false);
                if (existing == null || !existing.DataType.Equals(dataType, StringComparison.Ordinal))
                {
                    return NotFound(typeof(Api.TrackLargeResourceItem).Name, trackResourceLargeItem.Id, nameof(trackResourceLargeItem.Id));
                }

                var mResource = mapper.Map<TrackLargeResource>(trackResourceLargeItem);
                NormalizeItems(mResource);

                if (mResource.Items.Count > 0)
                {
                    await tenantDataRepository.UpdateAsync(mResource);
                }
                else
                {
                    await tenantDataRepository.DeleteAsync<TrackLargeResource>(mResource.Id);
                }

                var apiResource = mapper.Map<Api.TrackLargeResourceItem>(mResource);
                OrderCultureItems(apiResource);
                return Ok(apiResource);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.TrackLargeResourceItem).Name}' by environment name '{RouteBinding.TrackName}' and id '{trackResourceLargeItem.Id}'.");
                    return NotFound(typeof(Api.TrackLargeResourceItem).Name, trackResourceLargeItem.Id, nameof(trackResourceLargeItem.Id));
                }
                throw;
            }
        }

        /// <summary>
        /// Delete a large resource.
        /// </summary>
        /// <param name="resourceId">Resource identifier.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteTrackLargeResource(string resourceId)
        {
            try
            {
                await tenantDataRepository.DeleteAsync<TrackLargeResource>(resourceId);
                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.TrackLargeResourceItem).Name}' by environment name '{RouteBinding.TrackName}' and id '{resourceId}'.");
                    return NotFound(typeof(Api.TrackLargeResourceItem).Name, resourceId);
                }
                throw;
            }
        }

        private static void OrderCultureItems(Api.TrackLargeResourceItem resource)
        {
            if (resource?.Items != null)
            {
                resource.Items = resource.Items.OrderBy(i => i.Culture).ToList();
            }
        }

        private static void ValidateCultures(Api.TrackLargeResourceItem resource)
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

        private static void NormalizeItems(TrackLargeResource resource)
        {
            if (resource.Items == null)
            {
                resource.Items = new List<TrackLargeResourceCultureItem>();
            }

            resource.Items = resource.Items.Where(i => !i.Value.IsNullOrWhiteSpace()).ToList();
        }

        private async Task<bool> NameExistsAsync(Track.IdKey idKey, string name, string excludeId = null)
        {
            if (name.IsNullOrWhiteSpace())
            {
                return false;
            }

            var paginationToken = (string)null;
            do
            {
                (var mResources, var nextPaginationToken) = await tenantDataRepository.GetManyAsync<TrackLargeResource>(idKey, whereQuery: r => r.DataType.Equals(dataType), paginationToken: paginationToken);
                if (mResources.Any(r => r.Name != null && r.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) && (excludeId == null || !r.Id.Equals(excludeId, StringComparison.Ordinal))))
                {
                    return true;
                }

                paginationToken = nextPaginationToken;
            }
            while (!paginationToken.IsNullOrWhiteSpace());

            return false;
        }
    }
}
