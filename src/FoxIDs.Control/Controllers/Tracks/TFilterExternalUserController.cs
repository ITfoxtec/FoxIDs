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
    [Obsolete($"Use {nameof(TExternalUsersController)} instead.")]
    public class TFilterExternalUserController : ApiController
    {
        private const string dataType = Constants.Models.DataType.ExternalUser;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TFilterExternalUserController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Obsolete please use 'ExternalUsers' instead.
        /// Filter external user.
        /// </summary>
        /// <param name="filterValue">Filter external user by link claim or user ID.</param>
        /// <returns>External users.</returns>
        [ProducesResponseType(typeof(HashSet<Api.ExternalUser>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Obsolete($"Use {nameof(TExternalUsersController)} instead.")]
        public async Task<ActionResult<HashSet<Api.ExternalUser>>> GetFilterExternalUser(string filterValue)
        {
            try
            {
                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                (var mExternalUsers, _) = filterValue.IsNullOrWhiteSpace() ? 
                    await tenantDataRepository.GetManyAsync<ExternalUser>(idKey, whereQuery: u => u.DataType.Equals(dataType)) : 
                    await tenantDataRepository.GetManyAsync<ExternalUser>(idKey, whereQuery: u => u.DataType.Equals(dataType) &&
                        ((u.LinkClaimValue != null && u.LinkClaimValue.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase)) ||
                        (u.RedemptionClaimValue != null && u.RedemptionClaimValue.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase)) ||
                        u.UserId.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase)));

                var aExternalUsers = new HashSet<Api.ExternalUser>(mExternalUsers.Count());
                foreach(var mUser in mExternalUsers.OrderBy(t => t.RedemptionClaimValue ?? t.LinkClaimValue))
                {
                    aExternalUsers.Add(mapper.Map<Api.ExternalUser>(mUser));
                }
                return Ok(aExternalUsers);
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
