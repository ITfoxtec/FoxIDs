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
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly PlanCacheLogic planCacheLogic;
        private readonly BaseAccountLogic accountLogic;
        private readonly TenantApiLockLogic tenantApiLockLogic;

        public TUsersController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, PlanCacheLogic planCacheLogic, BaseAccountLogic accountLogic, TenantApiLockLogic tenantApiLockLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.planCacheLogic = planCacheLogic;
            this.accountLogic = accountLogic;
            this.tenantApiLockLogic = tenantApiLockLogic;
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
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.User>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Api.PaginationResponse<Api.User>>> GetUsers(string filterEmail = null, string filterPhone = null, string filterUsername = null, string filterUserId = null, string paginationToken = null)
        {
            try
            {
                filterEmail = filterEmail?.Trim();
                filterPhone = filterPhone?.Trim();
                filterUsername = filterUsername?.Trim();
                filterUserId = filterUserId?.Trim();
                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };

                var whereQuery = LinqFilterExpression.CreateUserFilterExpression(filterEmail, filterPhone, filterUsername, filterUserId);

                (var mUsers, var nextPaginationToken) = await tenantDataRepository.GetManyAsync<User>(idKey, whereQuery: whereQuery, paginationToken: paginationToken);
      
                var response = new Api.PaginationResponse<Api.User>
                {
                    Data = new HashSet<Api.User>(mUsers.Count()),
                    PaginationToken = nextPaginationToken,
                };
                foreach(var mUser in mUsers.OrderBy(t => t.Email ?? t.Username ?? t.Phone))
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

        /// <summary>
        /// Create new users or override existing users. It is not possible to update username/phone/email user identifies in this API method.
        /// </summary>
        /// <param name="usersRequest">Users.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status423Locked)]
        public async Task<IActionResult> PutUsers([FromBody] Api.UsersRequest usersRequest)
        {
            if (!await ModelState.TryValidateObjectAsync(usersRequest)) return BadRequest(ModelState);

            TenantApiLock tenantLock = null;
            try
            {
                if (!RouteBinding.PlanName.IsNullOrEmpty())
                {
                    var plan = await planCacheLogic.GetPlanAsync(RouteBinding.PlanName);
                    if (plan.Users.LimitedThreshold > 0)
                    {
                        tenantLock = await tenantApiLockLogic.AcquireAsync(RouteBinding.TenantName, TenantApiLock.UsersScope);
                        if (tenantLock == null)
                        {
                            return Locked(
                                $"User operations are currently locked for tenant '{RouteBinding.TenantName}'. Please retry shortly.",
                                $"Tenant API lock for tenant '{RouteBinding.TenantName}' and scope '{TenantApiLock.UsersScope}' could not be acquired when creating {usersRequest.Users.Count} user(s).");
                        }

                        Expression<Func<User, bool>> whereQuery = p => p.DataType.Equals("user") && p.PartitionId.StartsWith($"{RouteBinding.TenantName}:");
                        var count = await tenantDataRepository.CountAsync(whereQuery: whereQuery, usePartitionId: false);
                        // included + one master user
                        if (count + usersRequest.Users.Count > plan.Users.LimitedThreshold)
                        {
                            throw new PlanException(plan, $"Maximum number of users ({plan.Users.LimitedThreshold}) in the '{plan.Name}' plan has been reached.");
                        }
                    }
                }

                var mUsers = new List<User>();
                foreach (var user in usersRequest.Users)
                {
                    var mUser = await accountLogic.CreateUserAsync(new CreateUserObj
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
                        DisableSetPasswordSms = user.DisableSetPasswordSms,
                        DisableSetPasswordEmail = user.DisableSetPasswordEmail,
                        RequireMultiFactor = user.RequireMultiFactor
                    }, saveUser: false);

                    if (!user.PasswordHashAlgorithm.IsNullOrWhiteSpace())
                    {
                        mUser.ChangePassword = user.ChangePassword;
                        mUser.HashAlgorithm = user.PasswordHashAlgorithm;
                        mUser.Hash = user.PasswordHash;
                        mUser.HashSalt = user.PasswordHashSalt;
                    }

                    mUsers.Add(mUser);
                }

                await tenantDataRepository.SaveManyAsync(mUsers);

                return NoContent();
            }
            finally
            {
                if (tenantLock != null)
                {
                    await tenantApiLockLogic.ReleaseAsync(tenantLock);
                }
            }
        }

        /// <summary>
        /// Delete users.
        /// </summary>
        /// <param name="usersDelete">Delete specified users.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUsers([FromBody] Api.UsersDelete usersDelete)
        {
            if (!await ModelState.TryValidateObjectAsync(usersDelete)) return BadRequest(ModelState);

            var ids = new List<string>();
            foreach (var userIdentifier in usersDelete.UserIdentifiers)
            {
                ids.ConcatOnce(await Models.User.IdFormatAsync(RouteBinding, new User.IdKey { UserIdentifier = userIdentifier }));
            }

            if (ids.Count() <= 0)
            {
                throw new Exception("User identifiers is empty.");
            }
            await tenantDataRepository.DeleteManyAsync<User>(ids, queryAdditionalIds: true);

            return NoContent();
        }
    }
}
