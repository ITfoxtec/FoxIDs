using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models;
using FoxIDs.Repository;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using System.Collections.Generic;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.User)]
    public class TUserPasswordHistoryController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TUserPasswordHistoryController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Get user password history.
        /// </summary>
        [ProducesResponseType(typeof(Api.UserPasswordHistory), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.UserPasswordHistory>> GetUserPasswordHistory(string email = null, string phone = null, string username = null)
        {
            try
            {
                if (email.IsNullOrWhiteSpace() && phone.IsNullOrWhiteSpace() && username.IsNullOrWhiteSpace())
                {
                    ModelState.TryAddModelError(string.Empty, $"The {nameof(email)} or {nameof(phone)} or {nameof(username)} parameter is required.");
                    return BadRequest(ModelState);
                }
                email = email.IsNullOrWhiteSpace() ? null : email.Trim().ToLower();
                phone = phone.IsNullOrWhiteSpace() ? null : phone.Trim();
                username = username.IsNullOrWhiteSpace() ? null : username.Trim().ToLower();

                var mUser = await tenantDataRepository.GetAsync<User>(await Models.User.IdFormatAsync(RouteBinding, new User.IdKey { Email = email, UserIdentifier = phone ?? username }), queryAdditionalIds: true);
                return Ok(mapper.Map<Api.UserPasswordHistory>(mUser));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.UserPasswordHistory).Name}' by email '{email}', phone '{phone}', username '{username}'.");
                    return NotFound(typeof(Api.UserPasswordHistory).Name, new { email, phone, username }.ToJson());
                }
                throw;
            }
        }

        /// <summary>
        /// Replace user password history.
        /// </summary>
        [ProducesResponseType(typeof(Api.UserPasswordHistory), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.UserPasswordHistory>> PutUserPasswordHistory([FromBody] Api.UserPasswordHistoryRequest request)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(request)) return BadRequest(ModelState);
                request.Email = request.Email?.Trim().ToLower();
                request.Phone = request.Phone?.Trim();
                request.Username = request.Username?.Trim()?.ToLower();

                var mUser = await tenantDataRepository.GetAsync<User>(await Models.User.IdFormatAsync(RouteBinding, new User.IdKey { Email = request.Email, UserIdentifier = request.Phone ?? request.Username }), queryAdditionalIds: true);
                mUser.PasswordHistory = mapper.Map<List<PasswordHistoryItem>>(request.PasswordHistory);
                await tenantDataRepository.SaveAsync(mUser);

                return Ok(mapper.Map<Api.UserPasswordHistory>(mUser));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.UserPasswordHistory).Name}' by email '{request.Email}', phone '{request.Phone}', username '{request.Username}'.");
                    return NotFound(typeof(Api.UserPasswordHistory).Name, new { request.Email, request.Phone, request.Username }.ToJson());
                }
                throw;
            }
        }

        /// <summary>
        /// Delete user password history.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUserPasswordHistory(string email = null, string phone = null, string username = null)
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
                mUser.PasswordHistory = null;
                await tenantDataRepository.SaveAsync(mUser);

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.UserPasswordHistory).Name}' by email '{email}', phone '{phone}', username '{username}'.");
                    return NotFound(typeof(Api.UserPasswordHistory).Name, new { email, phone, username }.ToJson());
                }
                throw;
            }
        }
    }
}
