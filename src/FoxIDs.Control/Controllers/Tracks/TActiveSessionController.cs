using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Api = FoxIDs.Models.Api;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.User)]
    public class TActiveSessionController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly OAuthRefreshTokenGrantDownBaseLogic oauthRefreshTokenGrantDownBaseLogic;

        public TActiveSessionController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, OAuthRefreshTokenGrantDownBaseLogic oauthRefreshTokenGrantDownBaseLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.oauthRefreshTokenGrantDownBaseLogic = oauthRefreshTokenGrantDownBaseLogic;
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

                if (!sessionId.EndsWith(Constants.Models.Session.ShortSessionPostKey, StringComparison.Ordinal))
                {
                    throw new FoxIDsDataException() { StatusCode = DataStatusCode.NotFound };
                }

                var idKey = new ActiveSessionTtl.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, SessionIdHash = await sessionId.HashIdStringAsync() };
                var session = await tenantDataRepository.GetAsync<ActiveSessionTtl>(await ActiveSessionTtl.IdFormatAsync(idKey));

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

                if (!sessionId.EndsWith(Constants.Models.Session.ShortSessionPostKey, StringComparison.Ordinal))
                {
                    throw new FoxIDsDataException() { StatusCode = DataStatusCode.NotFound };
                }

                await oauthRefreshTokenGrantDownBaseLogic.DeleteRefreshTokenGrantsAsync(null, sessionId: sessionId);

                var idKey = new ActiveSessionTtl.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, SessionIdHash = await sessionId.HashIdStringAsync() };
                await tenantDataRepository.DeleteAsync<ActiveSessionTtl>(await ActiveSessionTtl.IdFormatAsync(idKey));

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
