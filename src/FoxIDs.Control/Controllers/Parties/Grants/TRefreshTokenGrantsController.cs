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
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Party)]
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
        /// <param name="filterUserIdentifier">Filter by the user identifier which can be: sub, email, phone or username.</param>
        /// <param name="filterClientId">Filter by the applications client ID.</param>
        /// <param name="paginationToken">The pagination token.</param>
        /// <returns>Refresh token grants.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.RefreshTokenGrant>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.PaginationResponse<Api.RefreshTokenGrant>>> GetRefreshTokenGrants(string filterUserIdentifier, string filterClientId, string paginationToken = null)
        {
            try
            {
                filterUserIdentifier = filterUserIdentifier?.Trim().ToLower();
                filterClientId = filterClientId?.Trim().ToLower();

                (var mTtlGrants, var mGrants, var nextPaginationToken) = await oauthRefreshTokenGrantDownBaseLogic.ListRefreshTokenGrantsByUserIdentifierAndClientIdAsync(filterUserIdentifier, filterClientId, paginationToken);
                
                var response = new Api.PaginationResponse<Api.RefreshTokenGrant>
                {
                    Data = new HashSet<Api.RefreshTokenGrant>(mTtlGrants.Count() + mGrants.Count()),
                    PaginationToken = nextPaginationToken,
                };
                foreach(var mTttlGrant in mTtlGrants)
                {
                    response.Data.Add(mapper.Map<Api.RefreshTokenGrant>(mTttlGrant));
                }
                foreach (var mGrant in mGrants)
                {
                    response.Data.Add(mapper.Map<Api.RefreshTokenGrant>(mGrant));
                }
                return Ok(response);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.RefreshTokenGrant).Name}' by filter user identifier '{filterUserIdentifier}', client ID '{filterClientId}'.");
                    return NotFound(typeof(Api.RefreshTokenGrant).Name, new { filterClientId, filterUserIdentifier }.ToJson());
                }
                throw;
            }
        }
    }
}
