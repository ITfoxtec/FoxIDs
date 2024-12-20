using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Logic;
using System.Security.Claims;
using System.Collections.Generic;
using ITfoxtec.Identity;
using System;
using System.Linq.Expressions;
using FoxIDs.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.User)]
    public class TUserController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly PlanCacheLogic planCacheLogic;
        private readonly BaseAccountLogic accountLogic;

        public TUserController(TelemetryScopedLogger logger, IServiceProvider serviceProvider, IMapper mapper, ITenantDataRepository tenantDataRepository, PlanCacheLogic planCacheLogic, BaseAccountLogic accountLogic) : base(logger)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.planCacheLogic = planCacheLogic;
            this.accountLogic = accountLogic;
        }

        /// <summary>
        /// Get user.
        /// </summary>
        /// <param name="email">User email.</param>
        /// <returns>User.</returns>
        [ProducesResponseType(typeof(Api.User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.User>> GetUser(string email)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(email, nameof(email))) return BadRequest(ModelState);

                var mUser = await tenantDataRepository.GetAsync<User>(await Models.User.IdFormatAsync(RouteBinding, new User.IdKey { UserIdentifier = email?.ToLower() }));
                return Ok(mapper.Map<Api.User>(mUser));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.User).Name}' by email '{email}'.");
                    return NotFound(typeof(Api.User).Name, email);
                }
                throw;
            }
        }

        /// <summary>
        /// Create user.
        /// </summary>
        /// <param name="createUserRequest">User.</param>
        /// <returns>User.</returns>
        [ProducesResponseType(typeof(Api.User), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.User>> PostUser([FromBody] Api.CreateUserRequest createUserRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(createUserRequest)) return BadRequest(ModelState);
                createUserRequest.Email = createUserRequest.Email?.ToLower();

                if (!RouteBinding.PlanName.IsNullOrEmpty())
                {
                    var plan = await planCacheLogic.GetPlanAsync(RouteBinding.PlanName);
                    if (plan.Users.LimitedThreshold > 0)
                    {
                        Expression<Func<User, bool>> whereQuery = p => p.DataType.Equals("user") && p.PartitionId.StartsWith($"{RouteBinding.TenantName}:");
                        var count = await tenantDataRepository.CountAsync(whereQuery: whereQuery, usePartitionId: false);
                        // included + one master user
                        if (count > plan.Users.LimitedThreshold)
                        {
                            throw new Exception($"Maximum number of users ({plan.Users.LimitedThreshold}) in the '{plan.Name}' plan has been reached.");
                        }
                    }
                }

                var claims = new List<Claim>();
                if (createUserRequest.Claims?.Count > 0)
                {
                    foreach (var claimAndValue in createUserRequest.Claims)
                    {
                        foreach(var value in claimAndValue.Values)
                        {
                            claims.Add(new Claim(claimAndValue.Claim, value));
                        }
                    }
                }
                var mUser = await accountLogic.CreateUser(createUserRequest.Email, createUserRequest.Password, changePassword: createUserRequest.ChangePassword, claims: claims, 
                    confirmAccount: createUserRequest.ConfirmAccount, emailVerified: createUserRequest.EmailVerified, disableAccount: createUserRequest.DisableAccount, requireMultiFactor: createUserRequest.RequireMultiFactor);
                return Created(mapper.Map<Api.User>(mUser));
            }
            catch(UserExistsException ueex)
            {
                logger.Warning(ueex, $"Conflict, Create '{typeof(Api.User).Name}' by email '{createUserRequest.Email}'.");
                return Conflict(ueex.Message);
            }
            catch (AccountException aex)
            {
                ModelState.TryAddModelError(nameof(createUserRequest.Password), aex.Message);
                return BadRequest(ModelState, aex);
            }            
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create '{typeof(Api.User).Name}' by email '{createUserRequest.Email}'.");
                    return Conflict(typeof(Api.User).Name, createUserRequest.Email, nameof(createUserRequest.Email));
                }
                throw;
            }
        }

        /// <summary>
        /// Update user.
        /// </summary>
        /// <param name="user">User.</param>
        /// <returns>User.</returns>
        [ProducesResponseType(typeof(Api.User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.User>> PutUser([FromBody] Api.UserRequest user)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(user)) return BadRequest(ModelState);
                user.Email = user.Email?.ToLower();

                var mUser = await tenantDataRepository.GetAsync<User>(await Models.User.IdFormatAsync(RouteBinding, new User.IdKey { UserIdentifier = user.Email }));

                mUser.ConfirmAccount = user.ConfirmAccount;
                mUser.EmailVerified = user.EmailVerified;
                mUser.ChangePassword = user.ChangePassword;
                mUser.DisableAccount = user.DisableAccount;
                if (!user.ActiveTwoFactorApp)
                {
                    if (!mUser.TwoFactorAppSecretExternalName.IsNullOrEmpty())
                    {
                        try
                        {
                            await serviceProvider.GetService<ExternalSecretLogic>().DeleteExternalSecretAsync(mUser.TwoFactorAppSecretExternalName);
                        }
                        catch (Exception ex)
                        {
                            logger.Warning(ex, $"Unable to delete external secret, secretExternalName '{mUser.TwoFactorAppSecretExternalName}'.");
                        }
                    }

                    mUser.TwoFactorAppSecret = null;
                    mUser.TwoFactorAppSecretExternalName = null;
                    mUser.TwoFactorAppRecoveryCode = null;
                }
                mUser.RequireMultiFactor = user.RequireMultiFactor;
                var mClaims = mapper.Map<List<ClaimAndValues>>(user.Claims);
                mUser.Claims = mClaims;
                await tenantDataRepository.UpdateAsync(mUser);

                return Ok(mapper.Map<Api.User>(mUser));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.UserRequest).Name}' by email '{user.Email}'.");
                    return NotFound(typeof(Api.UserRequest).Name, user.Email, nameof(user.Email));
                }
                throw;
            }
        }

        /// <summary>
        /// Delete user.
        /// </summary>
        /// <param name="email">User email.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(string email)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(email, nameof(email))) return BadRequest(ModelState);
                email = email?.ToLower();

                await tenantDataRepository.DeleteAsync<User>(await Models.User.IdFormatAsync(RouteBinding, new User.IdKey { UserIdentifier = email }));
                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.User).Name}' by email '{email}'.");
                    return NotFound(typeof(Api.User).Name, email);
                }
                throw;
            }
        }
    }
}
