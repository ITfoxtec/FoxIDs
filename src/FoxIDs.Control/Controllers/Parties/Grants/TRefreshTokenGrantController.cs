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
        /// <param name="refreshToken">The refresh token.</param>
        /// <returns>Refresh token grant.</returns>
        [ProducesResponseType(typeof(Api.RefreshTokenGrant), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.RefreshTokenGrant>> GetRefreshTokenGrant(string refreshToken)
        {
            try
            {
                if (refreshToken.IsNullOrWhiteSpace())
                {
                    ModelState.TryAddModelError(string.Empty, $"The {nameof(refreshToken)} parameter is required.");
                    return BadRequest(ModelState);
                }

                (var ttlGrant, var grant) = await oauthRefreshTokenGrantDownBaseLogic.GetRefreshTokenGrantAsync(refreshToken);

                return Ok(mapper.Map<Api.RefreshTokenGrant>(ttlGrant ?? grant));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.RefreshTokenGrant).Name}' by refresh token '{refreshToken}'.");
                    return NotFound(typeof(Api.RefreshTokenGrant).Name, refreshToken);
                }
                throw;
            }
        }

        /// <summary>
        /// Delete refresh token grant.
        /// </summary>
        /// <param name="refreshToken">The refresh token.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteRefreshTokenGrants(string refreshToken)
        {
            try
            {
                if (refreshToken.IsNullOrWhiteSpace())
                {
                    ModelState.TryAddModelError(string.Empty, $"The {nameof(refreshToken)} parameter is required.");
                    return BadRequest(ModelState);
                }

                await oauthRefreshTokenGrantDownBaseLogic.DeleteRefreshTokenGrantAsync(refreshToken);

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.RefreshTokenGrant).Name}' by refresh token '{refreshToken}'.");
                    return NotFound(typeof(Api.RefreshTokenGrant).Name, refreshToken);
                }
                throw;
            }
        }
    }
}
