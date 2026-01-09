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
        /// <param name="filterValue">Filter external user by link claim, redemption claim, or user ID.</param>
        /// <param name="filterClaimValue">Filter external user by claim value.</param>
        /// <param name="paginationToken">The pagination token.</param>
        /// <returns>External users.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.ExternalUser>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.PaginationResponse<Api.ExternalUser>>> GetExternalUsers(string filterValue, string filterClaimValue = null, string paginationToken = null)
        {
            try
            {
                filterValue = filterValue?.Trim();
                filterClaimValue = filterClaimValue?.Trim();
                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };

                (var mExternalUsers, var nextPaginationToken) = await QueryExternalUsersInternal(filterValue, filterClaimValue, paginationToken, idKey);

                var response = new Api.PaginationResponse<Api.ExternalUser>
                {
                    Data = new HashSet<Api.ExternalUser>(mExternalUsers.Count()),
                    PaginationToken = nextPaginationToken,
                };
                foreach (var mUser in mExternalUsers.OrderBy(t => t.RedemptionClaimValue ?? t.LinkClaimValue))
                {
                    response.Data.Add(mapper.Map<Api.ExternalUser>(mUser));
                }
                return Ok(response);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    var filterSummary = string.Join(", ", new[] { filterValue, filterClaimValue }.Where(value => !value.IsNullOrWhiteSpace()));
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.ExternalUser).Name}' by filter value '{filterSummary}'.");
                    return NotFound(typeof(Api.ExternalUser).Name, filterSummary);
                }
                throw;
            }
        }

        private async Task<(IReadOnlyCollection<ExternalUser> mExternalUsers, string nextPaginationToken)> QueryExternalUsersInternal(string filterValue, string filterClaimValue, string paginationToken, Track.IdKey idKey)
        {
            if (filterValue.IsNullOrWhiteSpace() && filterClaimValue.IsNullOrWhiteSpace())
            {
                return await tenantDataRepository.GetManyAsync<ExternalUser>(idKey, whereQuery: u => u.DataType.Equals(dataType), paginationToken: paginationToken);
            }
            else if (filterClaimValue.IsNullOrWhiteSpace())
            {
                return await tenantDataRepository.GetManyAsync<ExternalUser>(idKey, whereQuery: u => u.DataType.Equals(dataType) &&
                    ((u.LinkClaimValue != null && u.LinkClaimValue.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase)) ||
                    (u.RedemptionClaimValue != null && u.RedemptionClaimValue.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase)) ||
                    u.UserId.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase)), paginationToken: paginationToken);
            }
            else if (filterValue.IsNullOrWhiteSpace())
            {
                return await tenantDataRepository.GetManyAsync<ExternalUser>(idKey, whereQuery: u => u.DataType.Equals(dataType) &&
                    u.Claims != null && u.Claims.Any(c => c.Values.Any(v => v.Contains(filterClaimValue, StringComparison.CurrentCultureIgnoreCase))), paginationToken: paginationToken);
            }
            else
            {
                return await tenantDataRepository.GetManyAsync<ExternalUser>(idKey, whereQuery: u => u.DataType.Equals(dataType) &&
                    ((u.LinkClaimValue != null && u.LinkClaimValue.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase)) ||
                    (u.RedemptionClaimValue != null && u.RedemptionClaimValue.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase)) ||
                    u.UserId.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase) ||
                    (u.Claims != null && u.Claims.Any(c => c.Values.Any(v => v.Contains(filterClaimValue, StringComparison.CurrentCultureIgnoreCase))))), paginationToken: paginationToken);
            }
        }
    }
}
