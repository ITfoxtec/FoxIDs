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
using FoxIDs.Logic;
using FoxIDs.Models.Logic;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.User)]
    public class TUsersController : ApiController
    {
        private const string dataType = Constants.Models.DataType.User;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly PlanCacheLogic planCacheLogic;
        private readonly BaseAccountLogic accountLogic;

        public TUsersController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, PlanCacheLogic planCacheLogic, BaseAccountLogic accountLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.planCacheLogic = planCacheLogic;
            this.accountLogic = accountLogic;
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
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.User).Name}' by filter email '{filterEmail}', phone '{filterPhone}', username '{filterUsername}', userId '{filterUserId}'.");
                    return NotFound(typeof(Api.User).Name, new { filterEmail, filterPhone, filterUsername, filterUserId }.ToJson());
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

        /// <summary>
        /// Create users if they do not already exist. Existing users are not updated and if a user exists, the update element is ignored.
        /// </summary>
        /// <param name="usersRequest">Users.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> PutUser([FromBody] Api.UsersRequest usersRequest)
        {
            if (!await ModelState.TryValidateObjectAsync(usersRequest)) return BadRequest(ModelState);

            if (!RouteBinding.PlanName.IsNullOrEmpty())
            {
                var plan = await planCacheLogic.GetPlanAsync(RouteBinding.PlanName);
                if (plan.Users.LimitedThreshold > 0)
                {
                    Expression<Func<User, bool>> whereQuery = p => p.DataType.Equals("user") && p.PartitionId.StartsWith($"{RouteBinding.TenantName}:");
                    var count = await tenantDataRepository.CountAsync(whereQuery: whereQuery, usePartitionId: false);
                    // included + one master user
                    if (count + usersRequest.Users.Count > plan.Users.LimitedThreshold)
                    {
                        throw new PlanException(plan, $"Maximum number of users ({plan.Users.LimitedThreshold}) in the '{plan.Name}' plan has been reached.");
                    }
                }
            }

            var users = new List<User>();
            foreach (var user in usersRequest.Users)
            {
                user.Email = user.Email?.Trim().ToLower();
                user.Phone = user.Phone?.Trim();
                user.Username = user.Username?.Trim()?.ToLower();

                if(!await tenantDataRepository.ExistsAsync<User>(await Models.User.IdFormatAsync(RouteBinding, new User.IdKey { Email = user.Email, UserIdentifier = user.Phone ?? user.Username }), queryAdditionalIds: true))
                {
                    _ = await accountLogic.CreateUserAsync(new CreateUserObj
                    {
                        UserIdentifier = new UserIdentifier { Email = user.Email, Phone = user.Phone, Username = user.Username },
                        Password = user.Password,
                        ChangePassword = user.Password.IsNullOrWhiteSpace() ? false : user.ChangePassword,
                        SetPasswordEmail = user.SetPasswordEmail,
                        SetPasswordSms = user.SetPasswordSms,
                        Claims = user.Claims.ToClaimList(),
                        ConfirmAccount = user.ConfirmAccount,
                        EmailVerified = user.EmailVerified,
                        PhoneVerified = user.PhoneVerified,
                        DisableAccount = user.DisableAccount,
                        DisableTwoFactorApp = user.DisableTwoFactorApp,
                        DisableTwoFactorSms = user.DisableTwoFactorSms,
                        DisableTwoFactorEmail = user.DisableTwoFactorEmail,
                        RequireMultiFactor = user.RequireMultiFactor
                    });
                }

                // Bulk user update is not supported.
            }

            return NoContent();
        }

        /// <summary>
        /// Delete users.
        /// </summary>
        /// <param name="usersDelete">Delete all users if empty. Alternatively, select to delete specific users.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser([FromBody] Api.UsersDelete usersDelete = null)
        {
            if (usersDelete == null)
            {
                await tenantDataRepository.DeleteBulkAsync<User>();
            }
            else
            {
                if (!await ModelState.TryValidateObjectAsync(usersDelete)) return BadRequest(ModelState);

                var ids = new List<string>();
                foreach (var userIdentifier in usersDelete.UserIdentifiers)
                {
                    ids.Add(await Models.User.IdFormatAsync(RouteBinding, new User.IdKey { UserIdentifier = userIdentifier }));
                }

                await tenantDataRepository.DeleteBulkAsync<User>(ids, queryAdditionalIds: true);
            }

            return NoContent();
        }
    }
}
