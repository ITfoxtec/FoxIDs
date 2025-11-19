using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Logic;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api = FoxIDs.Models.Api;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.User)]
    public class TActiveSessionsController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ActiveSessionLogic activeSessionLogic;

        public TActiveSessionsController(TelemetryScopedLogger logger, IMapper mapper, ActiveSessionLogic activeSessionLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.activeSessionLogic = activeSessionLogic;
        }

        /// <summary>
        /// Get active sessions.
        /// </summary>
        /// <param name="filterUserIdentifier">Filter by the user identifier which can be: email, phone or username.</param>
        /// <param name="filterSub">Filter by the users SUB claim.</param>
        /// <param name="filterUpPartyName">Filter by the authentication method.</param>
        /// <param name="filterDownPartyName">Filter by the application.</param>
        /// <param name="filterSessionId">Filter by the session ID.</param>
        /// <param name="paginationToken">The pagination token.</param>
        /// <returns>Active sessions.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.ActiveSession>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.PaginationResponse<Api.ActiveSession>>> GetActiveSessions(string filterUserIdentifier, string filterSub, string filterUpPartyName, string filterDownPartyName, string filterSessionId, string paginationToken = null)
        {
            filterUserIdentifier = filterUserIdentifier?.Trim().ToLower();
            filterSub = filterSub?.Trim();
            filterUpPartyName = filterUpPartyName?.Trim().ToLower();
            filterDownPartyName = filterDownPartyName?.Trim().ToLower();
            filterSessionId = filterSessionId?.Trim();

            (var mSessions, var nextPaginationToken) = await activeSessionLogic.ListSessionsAsync(filterUserIdentifier, filterSub, filterUpPartyName, filterDownPartyName, filterSessionId, paginationToken: paginationToken);

            var response = new Api.PaginationResponse<Api.ActiveSession>
            {
                Data = mSessions?.Select(s =>
                {
                    var session = mapper.Map<Api.ActiveSession>(s);
                    return session;
                }).OrderByDescending(s => s.CreateTime).ToHashSet() ?? new HashSet<Api.ActiveSession>(),
                PaginationToken = nextPaginationToken
            };

            return Ok(response);
        }

        /// <summary>
        /// Delete active sessions.
        /// </summary>
        /// <param name="userIdentifier">User identifier which can be: email, phone or username.</param>
        /// <param name="sub">The users SUB claim.</param>
        /// <param name="upPartyName">Filter by the authentication method.</param>
        /// <param name="downPartyName">Filter by the application.</param>
        /// <param name="sessionId">Filter by session ID.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteActiveSessions(string userIdentifier = null, string sub = null, string upPartyName = null, string downPartyName = null, string sessionId = null)
        {
            try
            {
                if (userIdentifier.IsNullOrWhiteSpace() && sub.IsNullOrWhiteSpace() && upPartyName.IsNullOrWhiteSpace() && downPartyName.IsNullOrWhiteSpace() && sessionId.IsNullOrWhiteSpace())
                {
                    ModelState.TryAddModelError(string.Empty, $"Either the {nameof(userIdentifier)} or the {nameof(sub)} or the {nameof(upPartyName)} or the {nameof(downPartyName)} or the {nameof(sessionId)} parameter is required.");
                    return BadRequest(ModelState);
                }

                userIdentifier = userIdentifier?.Trim().ToLower();
                sub = sub?.Trim();
                upPartyName = upPartyName?.Trim().ToLower();
                downPartyName = downPartyName?.Trim().ToLower();
                sessionId = sessionId?.Trim();

                await activeSessionLogic.DeleteSessionsAsync(userIdentifier, sub, upPartyName, downPartyName, sessionId);

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.ActiveSession).Name}' by user identifier '{userIdentifier}', SUB claim '{sub}', auth method '{upPartyName}', application '{downPartyName}', session ID '{sessionId}'.");
                    return NotFound(typeof(Api.ActiveSession).Name, new { userIdentifier, sub, upPartyName, downPartyName, sessionId }.ToJson());
                }
                throw;
            }
        }
    }
}
