using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AutoMapper;
using System.Collections.Generic;
using FoxIDs.Logic;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Controllers
{
    /// <summary>
    /// OIDC import client key for authentication method API.
    /// </summary>
    public class TOidcClientKeyUpPartyController : GenericOAuthClientKeyUpPartyController<OidcUpParty, OidcUpClient>
    {
        public TOidcClientKeyUpPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, PlanCacheLogic planCacheLogic) : base(logger, mapper, tenantDataRepository, planCacheLogic)
        { }

        /// <summary>
        /// Get OIDC client key for authentication method.
        /// </summary>
        /// <param name="partyName">OIDC authentication method name.</param>
        /// <returns>OIDC client key for authentication method.</returns>
        [ProducesResponseType(typeof(List<Api.OAuthClientKeyResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OAuthClientKeyResponse>> GetOidcClientKeyUpParty(string partyName) => await Get(partyName);

        /// <summary>
        /// Create OIDC client key for authentication method.
        /// </summary>
        /// <param name="keyRequest">OIDC client key for authentication method.</param>
        [ProducesResponseType(typeof(Api.OAuthClientKeyResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OAuthClientKeyResponse>> PostOidcClientKeyUpParty([FromBody] Api.OAuthClientKeyRequest keyRequest) => await Post(keyRequest);

        /// <summary>
        /// Delete OIDC client key for authentication method.
        /// </summary>
        /// <param name="name">Name is [authentication method name].[key name] </param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOidcClientKeyUpParty(string name) => await Delete(name);
    }
}
