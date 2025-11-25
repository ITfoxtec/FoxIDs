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
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TTrackLargeResourcesController : ApiController
    {
        private const string dataType = Constants.Models.DataType.TrackLargeResource;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TTrackLargeResourcesController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
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
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.PaginationResponse<Api.TrackLargeResourceItem>>> GetTrackLargeResources(string filterName, string paginationToken = null)
        {
            try
            {
                filterName = filterName?.Trim();
                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                Expression<Func<TrackLargeResource, bool>> whereQuery = r => r.DataType.Equals(dataType);
                if (!filterName.IsNullOrWhiteSpace())
                {
                    whereQuery = r => r.DataType.Equals(dataType) && r.Name != null && r.Name.Contains(filterName, StringComparison.InvariantCultureIgnoreCase);
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

        private static void OrderCultureItems(Api.TrackLargeResourceItem resource)
        {
            if (resource?.Items != null)
            {
                resource.Items = resource.Items.OrderBy(i => i.Culture).ToList();
            }
        }
    }
}
