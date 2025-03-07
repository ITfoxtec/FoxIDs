using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Logic;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Party)]
    public class TRefreshTokenGrantController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly OAuthRefreshTokenGrantDownBaseLogic oauthRefreshTokenGrantDownBaseLogic;

        public TRefreshTokenGrantController(TelemetryScopedLogger logger, IMapper mapper, OAuthRefreshTokenGrantDownBaseLogic oauthRefreshTokenGrantDownBaseLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.oauthRefreshTokenGrantDownBaseLogic = oauthRefreshTokenGrantDownBaseLogic;
        }

        /// <summary>
        /// Get refresh token grant.
        /// </summary>
        /// <param name="userIdentifier">User identifier which can be: sub, email, phone or username.</param>
        /// <param name="clientId">Applications client ID.</param>
        /// <returns>Refresh token grant.</returns>
        [ProducesResponseType(typeof(Api.User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.RefreshTokenGrant>> GetRefreshTokenGrant(string userIdentifier, string clientId = null)
        {
            try
            {
                if (userIdentifier.IsNullOrWhiteSpace())
                {
                    ModelState.TryAddModelError(string.Empty, $"The {nameof(userIdentifier)} parameter is required.");
                    return BadRequest(ModelState);
                }
                userIdentifier = userIdentifier?.Trim().ToLower();
                clientId = clientId?.Trim().ToLower();

                (var ttlGrant, var grant) = await oauthRefreshTokenGrantDownBaseLogic.GetRefreshTokenGrantsByUserIdentifierAndClientIdAsync(userIdentifier, clientId);

                return Ok(mapper.Map<Api.RefreshTokenGrant>(ttlGrant ?? grant));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.RefreshTokenGrant).Name}' by user identifier '{userIdentifier}', client ID '{clientId}'.");
                    return NotFound(typeof(Api.RefreshTokenGrant).Name, new { userIdentifier, clientId }.ToJson());
                }
                throw;
            }
        }

        /// <summary>
        /// Delete refresh token grant.
        /// </summary>
        /// <param name="userIdentifier">User identifier which can be: sub, email, phone or username.</param>
        /// <param name="clientId">Applications client ID.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteRefreshTokenGrant(string userIdentifier, string clientId = null)
        {
            try
            {
                if (userIdentifier.IsNullOrWhiteSpace())
                {
                    ModelState.TryAddModelError(string.Empty, $"The {nameof(userIdentifier)} parameter is required.");
                    return BadRequest(ModelState);
                }
                userIdentifier = userIdentifier?.Trim().ToLower();
                clientId = clientId?.Trim().ToLower();

                await oauthRefreshTokenGrantDownBaseLogic.DeleteRefreshTokenGrantsByUserIdentifierAndClientIdAsync(userIdentifier, clientId);

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.RefreshTokenGrant).Name}' by user identifier '{userIdentifier}', client ID '{clientId}'.");
                    return NotFound(typeof(Api.RefreshTokenGrant).Name, new { userIdentifier, clientId }.ToJson());
                }
                throw;
            }
        }
    }
}
