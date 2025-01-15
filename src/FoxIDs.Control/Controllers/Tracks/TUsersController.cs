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
using System.Linq.Expressions;

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
                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };

                Expression<Func<User, bool>> whereQuery = u => u.DataType.Equals(dataType);
                var filterWhereQuerys = GetFilterWhereQuery(filterEmail, filterPhone, filterUsername, filterUserId);
                if (filterWhereQuerys?.Count() > 0)
                {
                    Expression<Func<User, bool>> filterWhereQuery = null; 
                    foreach (var fwq in filterWhereQuerys)
                    {
                        if (filterWhereQuery == null)
                        {
                            filterWhereQuery = fwq;
                        }
                        else
                        {
                            filterWhereQuery = filterWhereQuery.Or(fwq);
                        }
                    }
                    whereQuery = whereQuery.AndAlso(filterWhereQuery);
                }

                (var mUsers, var nextPaginationToken) = await tenantDataRepository.GetListAsync(idKey, whereQuery: whereQuery, paginationToken: paginationToken);
      
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

        private IEnumerable<Expression<Func<User, bool>>> GetFilterWhereQuery(string filterEmail, string filterPhone, string filterUsername, string filterUserId)
        {
            if (!filterEmail.IsNullOrWhiteSpace())
            {
                yield return u => u.Email.Contains(filterEmail, StringComparison.CurrentCultureIgnoreCase);
            }
            if (!filterPhone.IsNullOrWhiteSpace())
            {
                yield return u => u.Phone.Contains(filterPhone, StringComparison.CurrentCultureIgnoreCase);
            }
            if (!filterUsername.IsNullOrWhiteSpace())
            {
                yield return u => u.Username.Contains(filterUsername, StringComparison.CurrentCultureIgnoreCase);
            }
            if (!filterUserId.IsNullOrWhiteSpace())
            {
                yield return u => u.UserId.Contains(filterUserId, StringComparison.CurrentCultureIgnoreCase);
            }
        }
    }
}
