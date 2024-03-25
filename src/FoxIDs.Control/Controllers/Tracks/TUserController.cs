using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;
using FoxIDs.Logic;
using System.Security.Claims;
using System.Collections.Generic;
using ITfoxtec.Identity;
using System;
using System.Linq.Expressions;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.User)]
    public class TUserController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;
        private readonly PlanCacheLogic planCacheLogic;
        private readonly BaseAccountLogic accountLogic;
        private readonly ExternalSecretLogic externalSecretLogic;

        public TUserController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, PlanCacheLogic planCacheLogic, BaseAccountLogic accountLogic, ExternalSecretLogic externalSecretLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
            this.planCacheLogic = planCacheLogic;
            this.accountLogic = accountLogic;
            this.externalSecretLogic = externalSecretLogic;
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

                var mUser = await tenantRepository.GetAsync<User>(await Models.User.IdFormatAsync(RouteBinding, email?.ToLower()));
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
                    if (plan.Users.IsLimited)
                    {
                        Expression<Func<User, bool>> whereQuery = p => p.DataType.Equals("user") && p.PartitionId.StartsWith($"{RouteBinding.TenantName}:");
                        var count = await tenantRepository.CountAsync(whereQuery: whereQuery, usePartitionId: false);
                        // included + one master user
                        if (count > plan.Users.Included)
                        {
                            throw new Exception($"Maximum number of users ({plan.Users.Included}) included in the '{plan.Name}' plan has been reached.");
                        }
                    }

                    if (createUserRequest.RequireMultiFactor)
                    {                        
                        if (!plan.EnableKeyVault)
                        {
                            throw new Exception($"Key Vault and thereby two-factor authentication is not supported in the '{plan.Name}' plan.");
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

                if (!RouteBinding.PlanName.IsNullOrEmpty() && user.RequireMultiFactor)
                {
                    var plan = await planCacheLogic.GetPlanAsync(RouteBinding.PlanName);
                    if (!plan.EnableKeyVault)
                    {
                        throw new Exception($"Key Vault and thereby two-factor authentication is not supported in the '{plan.Name}' plan.");
                    }
                }

                var mUser = await tenantRepository.GetAsync<User>(await Models.User.IdFormatAsync(RouteBinding, user.Email));

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
                            await externalSecretLogic.DeleteExternalSecretAsync(mUser.TwoFactorAppSecretExternalName);
                        }
                        catch (Exception ex)
                        {
                            logger.Warning(ex, $"Unable to delete external secret, secretExternalName '{mUser.TwoFactorAppSecretExternalName}'.");
                        }
                    }

                    mUser.TwoFactorAppSecretExternalName = null;
                    mUser.TwoFactorAppRecoveryCode = null;
                }
                mUser.RequireMultiFactor = user.RequireMultiFactor;
                var mClaims = mapper.Map<List<ClaimAndValues>>(user.Claims);
                mUser.Claims = mClaims;
                await tenantRepository.UpdateAsync(mUser);

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

                await tenantRepository.DeleteAsync<User>(await Models.User.IdFormatAsync(RouteBinding, email));
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
