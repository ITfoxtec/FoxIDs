using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using ITfoxtec.Identity;
using System;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Logic;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.User)]
    public class TRefreshTokenGrantsController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly OAuthRefreshTokenGrantDownBaseLogic oauthRefreshTokenGrantDownBaseLogic;

        public TRefreshTokenGrantsController(TelemetryScopedLogger logger, IMapper mapper, OAuthRefreshTokenGrantDownBaseLogic oauthRefreshTokenGrantDownBaseLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.oauthRefreshTokenGrantDownBaseLogic = oauthRefreshTokenGrantDownBaseLogic;
        }

        /// <summary>
        /// Get refresh token grants.
        /// </summary>
        /// <param name="filterUserIdentifier">Filter by the user identifier which can be: email, phone or username.</param>
        /// <param name="filterSub">Filter by the users SUB claim.</param>
        /// <param name="filterClientId">Filter by the applications client ID.</param>
        /// <param name="filterAuthMethod">Filter by the authentication method.</param>
        /// <param name="paginationToken">The pagination token.</param>
        /// <returns>Refresh token grants.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.RefreshTokenGrant>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.PaginationResponse<Api.RefreshTokenGrant>>> GetRefreshTokenGrants(string filterUserIdentifier, string filterSub, string filterClientId, string filterAuthMethod, string paginationToken = null)
        {
            try
            {
                filterUserIdentifier = filterUserIdentifier?.Trim().ToLower();
                filterSub = filterSub?.Trim();
                filterClientId = filterClientId?.Trim().ToLower();
                filterAuthMethod = filterAuthMethod?.Trim().ToLower();

                (var mTtlGrants, var mGrants, var nextPaginationToken) = await oauthRefreshTokenGrantDownBaseLogic.ListRefreshTokenGrantsAsync(filterUserIdentifier, filterSub, filterClientId, filterAuthMethod, paginationToken);
                
                var response = new Api.PaginationResponse<Api.RefreshTokenGrant>
                {
                    Data = new HashSet<Api.RefreshTokenGrant>(mTtlGrants.Count() + mGrants.Count()),
                    PaginationToken = nextPaginationToken,
                };
                foreach(var mTtlGrant in mTtlGrants)
                {
                    var ttlGrant = mapper.Map<Api.RefreshTokenGrant>(mTtlGrant);
                    ttlGrant.Claims = null;
                    response.Data.Add(ttlGrant);
                }
                foreach (var mGrant in mGrants)
                {
                    var grant = mapper.Map<Api.RefreshTokenGrant>(mGrant);
                    grant.Claims = null;
                    response.Data.Add(grant);
                }
                return Ok(response);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.RefreshTokenGrant).Name}' by filter user identifier '{filterUserIdentifier}', client ID '{filterClientId}', auth method '{filterAuthMethod}'.");
                    return NotFound(typeof(Api.RefreshTokenGrant).Name, new { filterClientId, filterUserIdentifier, filterAuthMethod }.ToJson());
                }
                throw;
            }
        }

        /// <summary>
        /// Delete refresh token grants.
        /// </summary>
        /// <param name="userIdentifier">User identifier which can be: email, phone or username.</param>
        /// <param name="sub">The users SUB claim.</param>
        /// <param name="clientId">Applications client ID.</param>
        /// <param name="authMethod">Filter by the authentication method.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteRefreshTokenGrants(string userIdentifier = null, string sub = null, string clientId = null, string authMethod = null)
        {
            try
            {
                if (userIdentifier.IsNullOrWhiteSpace() && sub.IsNullOrWhiteSpace() && clientId.IsNullOrWhiteSpace() && authMethod.IsNullOrWhiteSpace())
                {
                    ModelState.TryAddModelError(string.Empty, $"Either the {nameof(userIdentifier)} or the {nameof(sub)} or the {nameof(clientId)} or the {nameof(authMethod)} parameter is required.");
                    return BadRequest(ModelState);
                }
                userIdentifier = userIdentifier?.Trim().ToLower();
                sub = sub?.Trim();
                clientId = clientId?.Trim().ToLower();
                authMethod = authMethod?.Trim().ToLower();

                await oauthRefreshTokenGrantDownBaseLogic.DeleteRefreshTokenGrantsAsync(userIdentifier, sub, clientId, authMethod);

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.RefreshTokenGrant).Name}' by user identifier '{userIdentifier}', client ID '{clientId}', auth method '{authMethod}'.");
                    return NotFound(typeof(Api.RefreshTokenGrant).Name, new { userIdentifier, clientId, authMethod }.ToJson());
                }
                throw;
            }
        }
    }
}
