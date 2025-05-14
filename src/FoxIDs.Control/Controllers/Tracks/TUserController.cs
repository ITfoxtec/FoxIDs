using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Logic;
using System.Collections.Generic;
using ITfoxtec.Identity;
using System;
using System.Linq.Expressions;
using FoxIDs.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.Models.Logic;
using System.Security.Claims;

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
        /// <param name="email">Users email.</param>
        /// <param name="phone">Users phone.</param>
        /// <param name="username">Users username.</param>
        /// <returns>User.</returns>
        [ProducesResponseType(typeof(Api.User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.User>> GetUser(string email = null, string phone = null, string username = null)
        {
            try
            {
                if (email.IsNullOrWhiteSpace() && phone.IsNullOrWhiteSpace() && username.IsNullOrWhiteSpace())
                {
                    ModelState.TryAddModelError(string.Empty, $"The {nameof(email)} or {nameof(phone)} or {nameof(username)} parameter is required.");
                    return BadRequest(ModelState);
                }
                email = email?.Trim().ToLower();
                phone = phone?.Trim();
                username = username?.Trim()?.ToLower();

                var mUser = await tenantDataRepository.GetAsync<User>(await Models.User.IdFormatAsync(RouteBinding, new User.IdKey { Email = email, UserIdentifier = phone ?? username }), queryAdditionalIds: true);
                return Ok(mapper.Map<Api.User>(mUser));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.User).Name}' by email '{email}', phone '{phone}', username '{username}'.");
                    return NotFound(typeof(Api.User).Name, new { email, phone, username }.ToJson());
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
                createUserRequest.Email = createUserRequest.Email?.Trim().ToLower();
                createUserRequest.Phone = createUserRequest.Phone?.Trim();
                createUserRequest.Username = createUserRequest.Username?.Trim()?.ToLower();

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
                            throw new PlanException(plan, $"Maximum number of users ({plan.Users.LimitedThreshold}) in the '{plan.Name}' plan has been reached.");
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

                var passwordless = createUserRequest.PasswordlessEmail || createUserRequest.PasswordlessSms;
                var mUser = await accountLogic.CreateUserAsync(new CreateUserObj
                {
                    UserIdentifier = new UserIdentifier { Email = createUserRequest.Email, Phone = createUserRequest.Phone, Username = createUserRequest.Username },
                    PasswordlessEmail = createUserRequest.PasswordlessEmail,
                    PasswordlessSms = createUserRequest.PasswordlessSms,
                    Password = passwordless ? null : createUserRequest.Password,
                    ChangePassword = passwordless ? false : createUserRequest.ChangePassword,
                    SetPasswordEmail = createUserRequest.SetPasswordEmail,
                    SetPasswordSms = createUserRequest.SetPasswordSms,
                    Claims = claims,
                    ConfirmAccount = createUserRequest.ConfirmAccount,
                    EmailVerified = createUserRequest.EmailVerified,
                    PhoneVerified = createUserRequest.PhoneVerified,
                    DisableAccount = createUserRequest.DisableAccount,
                    DisableTwoFactorApp = createUserRequest.DisableTwoFactorApp,
                    DisableTwoFactorSms = createUserRequest.DisableTwoFactorSms,
                    DisableTwoFactorEmail = createUserRequest.DisableTwoFactorEmail,
                    RequireMultiFactor = createUserRequest.RequireMultiFactor
                });
                return Created(mapper.Map<Api.User>(mUser));
            }
            catch(UserExistsException ueex)
            {
                logger.Warning(ueex, $"Conflict, Create '{typeof(Api.User).Name}' by email '{createUserRequest.Email}', phone '{createUserRequest.Phone}', username '{createUserRequest.Username}'.");
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
                    logger.Warning(ex, $"Conflict, Create '{typeof(Api.User).Name}' by email '{createUserRequest.Email}', phone '{createUserRequest.Phone}', username '{createUserRequest.Username}'.");
                    return Conflict(typeof(Api.User).Name, new { createUserRequest.Email, createUserRequest.Phone, createUserRequest.Username }.ToJson());
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
                user.Email = user.Email?.Trim().ToLower();
                user.Phone = user.Phone?.Trim();
                user.Username = user.Username?.Trim()?.ToLower();

                var mUser = await tenantDataRepository.GetAsync<User>(await Models.User.IdFormatAsync(RouteBinding, new User.IdKey { Email = user.Email, UserIdentifier = user.Phone ?? user.Username }), queryAdditionalIds: true);

                if (user.UpdateEmail != null)
                {
                    user.UpdateEmail = user.UpdateEmail?.Trim().ToLower();
                    if (user.UpdateEmail.IsNullOrEmpty())
                    {
                        mUser.Email = null;
                    }
                    else
                    {
                        mUser.Email = user.UpdateEmail;
                    }
                }
                if (user.UpdatePhone != null)
                {
                    user.UpdatePhone = user.UpdatePhone?.Trim();
                    if (user.UpdatePhone.IsNullOrEmpty())
                    {
                        mUser.Phone = null;
                    }
                    else
                    {
                        mUser.Phone = user.UpdatePhone;
                    }
                }
                if (user.UpdateUsername != null)
                {
                    user.UpdateUsername = user.UpdateUsername?.Trim().ToLower();
                    if (user.UpdateUsername.IsNullOrEmpty())
                    {
                        mUser.Username = null;
                    }
                    else
                    {
                        mUser.Username = user.UpdateUsername;
                    }
                }

                mUser.ConfirmAccount = user.ConfirmAccount;
                mUser.EmailVerified = mUser.Email.IsNullOrEmpty() ? false : user.EmailVerified;
                mUser.PhoneVerified = mUser.Phone.IsNullOrEmpty() ? false : user.PhoneVerified;

                if (mUser.PasswordlessEmail && !user.PasswordlessEmail && !user.PasswordlessSms)
                {
                    mUser.SetPasswordEmail = true;
                }
                else
                {
                    mUser.SetPasswordEmail = user.SetPasswordEmail;
                }
                if (mUser.PasswordlessSms && !user.PasswordlessEmail && !user.PasswordlessSms)
                {
                    mUser.SetPasswordSms = true;
                }
                else
                {
                    mUser.SetPasswordSms = user.SetPasswordSms;
                }
                mUser.PasswordlessEmail = user.PasswordlessEmail;
                mUser.PasswordlessSms = user.PasswordlessSms;

                mUser.ChangePassword = user.ChangePassword;
                mUser.DisableAccount = user.DisableAccount;
                mUser.DisableTwoFactorApp = user.DisableTwoFactorApp;
                mUser.DisableTwoFactorSms = user.DisableTwoFactorSms;
                mUser.DisableTwoFactorEmail = user.DisableTwoFactorEmail;
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

                if (user.UpdateEmail != null || user.UpdatePhone != null || user.UpdateUsername != null)
                {
                    mUser.AdditionalIds = null;
                    if (!mUser.Email.IsNullOrEmpty())
                    {
                        await mUser.SetAdditionalIdAsync(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, UserIdentifier = mUser.Email });
                    }
                    if (!mUser.Phone.IsNullOrEmpty())
                    {
                        await mUser.SetAdditionalIdAsync(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, UserIdentifier = mUser.Phone });
                    }
                    if (!mUser.Username.IsNullOrEmpty())
                    {
                        await mUser.SetAdditionalIdAsync(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, UserIdentifier = mUser.Username });
                    }
                }
                if (user.UpdateEmail != null && user.Email != user.UpdateEmail)
                {
                    var newId = await Models.User.IdFormatAsync(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Email = mUser.Email, UserId = mUser.UserId });
                    if (mUser.Id != newId)
                    {
                        await tenantDataRepository.DeleteAsync<User>(mUser.Id);
                        mUser.Id = newId;
                        await tenantDataRepository.CreateAsync(mUser);
                        return Ok(mapper.Map<Api.User>(mUser));
                    }
                }

                await tenantDataRepository.UpdateAsync(mUser);
                return Ok(mapper.Map<Api.User>(mUser));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.UserRequest).Name}' by email '{user.Email}', phone '{user.Phone}', username '{user.Username}'.");
                    return NotFound(typeof(Api.UserRequest).Name, new { user.Email, user.Phone, user.Username }.ToJson());
                }
                throw;
            }
        }

        /// <summary>
        /// Delete user.
        /// </summary>
        /// <param name="email">User email.</param>
        /// <param name="phone">User phone.</param>
        /// <param name="username">User username.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(string email, string phone, string username)
        {
            try
            {
                if (email.IsNullOrWhiteSpace() && phone.IsNullOrWhiteSpace() && username.IsNullOrWhiteSpace())
                {
                    ModelState.TryAddModelError(string.Empty, $"The {nameof(email)} or {nameof(phone)} or {nameof(username)} parameter is required.");
                    return BadRequest(ModelState);
                }
                email = email?.Trim().ToLower();
                phone = phone?.Trim();
                username = username?.Trim()?.ToLower();

                await tenantDataRepository.DeleteAsync<User>(await Models.User.IdFormatAsync(RouteBinding, new User.IdKey { Email = email, UserIdentifier = phone ?? username }), queryAdditionalIds: true);
                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.User).Name}' by email '{email}', phone '{phone}', username '{username}'.");
                    return NotFound(typeof(Api.User).Name, new { email, phone, username }.ToJson());
                }
                throw;
            }
        }
    }
}
