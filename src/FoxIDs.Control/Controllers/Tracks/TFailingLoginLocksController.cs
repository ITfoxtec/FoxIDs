using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using ITfoxtec.Identity;
using System;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models;
using FoxIDs.Models.Logic;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.User)]
    public class TFailingLoginLocksController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TFailingLoginLocksController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Get failing login locks.
        /// </summary>
        /// <param name="filterUserIdentifier">Filter by the user identifier.</param>
        /// <param name="filterFailingLoginType">Filter by the failing login type.</param>
        /// <param name="paginationToken">The pagination token.</param>
        /// <returns>Failing login locks.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.FailingLoginLock>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.PaginationResponse<Api.FailingLoginLock>>> GetFailingLoginLocks(string filterUserIdentifier, FailingLoginTypes? filterFailingLoginType = null, string paginationToken = null)
        {
            try
            {
                filterUserIdentifier = filterUserIdentifier?.Trim().ToLower();
                var queryByUserIdentifier = !filterUserIdentifier.IsNullOrWhiteSpace();
                var mFailingLoginType = filterFailingLoginType != null ? (FailingLoginTypes?)(int)filterFailingLoginType.Value : null;

                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                (var mFailingLoginLocks, var nextPaginationToken) = await tenantDataRepository.GetListAsync<FailingLoginLock>(idKey, d => d.DataType.Equals(Constants.Models.DataType.FailingLoginLock) &&
                            (!queryByUserIdentifier || d.UserIdentifier == filterUserIdentifier) &&
                            (!mFailingLoginType.HasValue || d.FailingLoginType == mFailingLoginType.Value), paginationToken: paginationToken);

                
                var response = new Api.PaginationResponse<Api.FailingLoginLock>
                {
                    Data = new HashSet<Api.FailingLoginLock>(mFailingLoginLocks.Count()),
                    PaginationToken = nextPaginationToken,
                };
                foreach(var mFailingLoginLock in mFailingLoginLocks)
                {
                    response.Data.Add(mapper.Map<Api.FailingLoginLock>(mFailingLoginLock));
                }
                return Ok(response);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.FailingLoginLock).Name}' by filter user identifier '{filterUserIdentifier}', failing login type '{filterFailingLoginType}'.");
                    return NotFound(typeof(Api.FailingLoginLock).Name, new { filterUserIdentifier, filterFailingLoginType }.ToJson());
                }
                throw;
            }
        }
    }
}
