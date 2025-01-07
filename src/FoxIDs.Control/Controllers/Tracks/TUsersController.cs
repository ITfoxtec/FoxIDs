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
    public class TUsersController : ApiController
    {
        private const string dataType = Constants.Models.DataType.User;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TUsersController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Get users.
        /// </summary>
        /// <param name="filterEmail">Filter by email.</param>
        /// <param name="filterPhone">Filter by phone.</param>
        /// <param name="filterUsername">Filter by username.</param>
        /// <param name="filterUserId">Filter by user ID.</param>
        /// <param name="paginationToken">The pagination token.</param>
        /// <returns>Users.</returns>
        [ProducesResponseType(typeof(HashSet<Api.User>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HashSet<Api.User>>> GetUsers(string filterEmail = null, string filterPhone = null, string filterUsername = null, string filterUserId = null, string paginationToken = null)
        {
            try
            {
                filterEmail = filterEmail.IsNullOrWhiteSpace() ? null : filterEmail;
                filterPhone = filterEmail.IsNullOrWhiteSpace() ? null : filterPhone;
                filterUsername = filterEmail.IsNullOrWhiteSpace() ? null : filterUsername;
                filterUserId = filterEmail.IsNullOrWhiteSpace() ? null : filterUserId;

                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                (var mUsers, var nextPaginationToken) = 
                    await tenantDataRepository.GetListAsync<User>(idKey, whereQuery: u => u.DataType.Equals(dataType) &&
                        (u.Email == null || u.Email.Contains(filterEmail, StringComparison.CurrentCultureIgnoreCase)) &&
                        (u.Phone == null || u.Phone.Contains(filterPhone, StringComparison.CurrentCultureIgnoreCase)) &&
                        (u.Username == null || u.Username.Contains(filterUsername, StringComparison.CurrentCultureIgnoreCase)) &&
                        (u.UserId == null || u.UserId.Contains(filterUserId, StringComparison.CurrentCultureIgnoreCase)), paginationToken: paginationToken);

                var response = new Api.PaginationResponse<Api.User>
                {
                    Data = new HashSet<Api.User>(mUsers.Count()),
                    PaginationToken = nextPaginationToken,
                };
                foreach(var mUser in mUsers.OrderBy(t => t.Email))
                {
                    response.Data.Add(mapper.Map<Api.User>(mUser));
                }
                return Ok(response);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.User).Name}' by filter email '{filterEmail}'.");
                    return NotFound(typeof(Api.User).Name, filterEmail);
                }
                throw;
            }
        }
    }
}
