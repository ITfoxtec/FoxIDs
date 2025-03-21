using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.User)]
    public class TFailingLoginLockController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly IMapper mapper;

        public TFailingLoginLockController(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, IMapper mapper) : base(logger)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.mapper = mapper;
        }

        /// <summary>
        /// Get failing login lock.
        /// </summary>
        /// <param name="userIdentifier">The user identifier.</param>
        /// <param name="failingLoginType">The failing login type.</param>
        /// <returns>Failing login.</returns>
        [ProducesResponseType(typeof(Api.RefreshTokenGrant), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.FailingLoginLock>> GetFailingLoginLock(string userIdentifier, Api.FailingLoginTypes failingLoginType)
        {
            try
            {
                if (userIdentifier.IsNullOrWhiteSpace())
                {
                    ModelState.TryAddModelError(string.Empty, $"The {nameof(userIdentifier)} parameter is required.");
                    return BadRequest(ModelState);
                }

                userIdentifier = userIdentifier?.Trim().ToLower();
                var mFailingLoginType = (FailingLoginTypes)(int)failingLoginType;

                var id = await FailingLoginLock.IdFormatAsync(new FailingLoginLock.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, UserIdentifier = userIdentifier, FailingLoginType = mFailingLoginType });
                var mFailingLoginLock = await tenantDataRepository.GetAsync<FailingLoginLock>(id);

                return Ok(mapper.Map<Api.FailingLoginLock>(mFailingLoginLock));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.FailingLoginLock).Name}' by user identifier '{userIdentifier}', failing login type '{failingLoginType}'.");
                    return NotFound(typeof(Api.FailingLoginLock).Name, new { userIdentifier, failingLoginType }.ToJson());
                }
                throw;
            }
        }

        /// <summary>
        /// Delete failing login lock.
        /// </summary>
        /// <param name="userIdentifier">The user identifier.</param>
        /// <param name="failingLoginType">The failing login type.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteFailingLoginLock(string userIdentifier, Api.FailingLoginTypes failingLoginType)
        {
            try
            {
                if (userIdentifier.IsNullOrWhiteSpace())
                {
                    ModelState.TryAddModelError(string.Empty, $"The {nameof(userIdentifier)} parameter is required.");
                    return BadRequest(ModelState);
                }

                userIdentifier = userIdentifier?.Trim().ToLower();
                var mFailingLoginType = (FailingLoginTypes)(int)failingLoginType;

                var id = await FailingLoginLock.IdFormatAsync(new FailingLoginLock.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, UserIdentifier = userIdentifier, FailingLoginType = mFailingLoginType });
                await tenantDataRepository.DeleteAsync<FailingLoginLock>(id);

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.FailingLoginLock).Name}' by user identifier '{userIdentifier}', failing login type '{failingLoginType}'.");
                    return NotFound(typeof(Api.FailingLoginLock).Name, new { userIdentifier, failingLoginType }.ToJson());
                }
                throw;
            }
        }
    }
}
