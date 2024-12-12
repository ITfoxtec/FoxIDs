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
    [TenantScopeAuthorize(Constants.ControlApi.Segment.User)]
    public class TExternalUsersController : ApiController
    {
        private const string dataType = Constants.Models.DataType.ExternalUser;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TExternalUsersController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Get external users.
        /// </summary>
        /// <param name="filterValue">Filter external user by link claim or user ID.</param>
        /// <param name="paginationToken">The pagination token.</param>
        /// <returns>External users.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.ExternalUser>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.PaginationResponse<Api.ExternalUser>>> GetExternalUsers(string filterValue, string paginationToken = null)
        {
            try
            {
                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                (var mExternalUsers, var nextPaginationToken) = filterValue.IsNullOrWhiteSpace() ? 
                    await tenantDataRepository.GetListAsync<ExternalUser>(idKey, whereQuery: u => u.DataType.Equals(dataType), paginationToken: paginationToken) : 
                    await tenantDataRepository.GetListAsync<ExternalUser>(idKey, whereQuery: u => u.DataType.Equals(dataType) && 
                        ((u.LinkClaimValue != null && u.LinkClaimValue.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase)) || 
                        (u.RedemptionClaimValue != null && u.RedemptionClaimValue.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase)) || 
                        u.UserId.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase)), paginationToken: paginationToken);

                var response = new Api.PaginationResponse<Api.ExternalUser>
                {
                    Data = new HashSet<Api.ExternalUser>(mExternalUsers.Count()),
                    PaginationToken = nextPaginationToken,
                };
                foreach(var mUser in mExternalUsers.OrderBy(t => t.RedemptionClaimValue ?? t.LinkClaimValue))
                {
                    response.Data.Add(mapper.Map<Api.ExternalUser>(mUser));
                }
                return Ok(response);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.ExternalUser).Name}' by filter value '{filterValue}'.");
                    return NotFound(typeof(Api.ExternalUser).Name, filterValue);
                }
                throw;
            }
        }
    }
}
