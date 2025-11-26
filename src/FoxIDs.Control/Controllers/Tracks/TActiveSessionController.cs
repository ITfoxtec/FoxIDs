using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Logic;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Api = FoxIDs.Models.Api;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.User)]
    public class TActiveSessionController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly OAuthRefreshTokenGrantDownBaseLogic oauthRefreshTokenGrantDownBaseLogic;
        private readonly ActiveSessionLogic activeSessionLogic;

        public TActiveSessionController(TelemetryScopedLogger logger, IMapper mapper, OAuthRefreshTokenGrantDownBaseLogic oauthRefreshTokenGrantDownBaseLogic, ActiveSessionLogic activeSessionLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.oauthRefreshTokenGrantDownBaseLogic = oauthRefreshTokenGrantDownBaseLogic;
            this.activeSessionLogic = activeSessionLogic;
        }

        /// <summary>
        /// Get active session.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        /// <returns>Active session.</returns>
        [ProducesResponseType(typeof(Api.ActiveSession), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.ActiveSession>> GetActiveSession(string sessionId)
        {
            try
            {
                if (sessionId.IsNullOrWhiteSpace())
                {
                    ModelState.TryAddModelError(string.Empty, $"The {nameof(sessionId)} parameter is required.");
                    return BadRequest(ModelState);
                }

                sessionId = sessionId.Trim();

                var session = await activeSessionLogic.GetSessionAsync(sessionId);

                return Ok(mapper.Map<Api.ActiveSession>(session));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.ActiveSession).Name}' by session ID '{sessionId}'.");
                    return NotFound(typeof(Api.ActiveSession).Name, sessionId);
                }
                throw;
            }
        }

        /// <summary>
        /// Delete active session.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteActiveSession(string sessionId)
        {
            try
            {
                if (sessionId.IsNullOrWhiteSpace())
                {
                    ModelState.TryAddModelError(string.Empty, $"The {nameof(sessionId)} parameter is required.");
                    return BadRequest(ModelState);
                }

                sessionId = sessionId.Trim();

                await oauthRefreshTokenGrantDownBaseLogic.DeleteRefreshTokenGrantsAsync(null, sessionId: sessionId);
                await activeSessionLogic.DeleteSessionAsync(sessionId);

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.ActiveSession).Name}' by session ID '{sessionId}'.");
                    return NotFound(typeof(Api.ActiveSession).Name, sessionId);
                }
                throw;
            }
        }
    }
}
