using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Controllers
{
    /// <summary>
    /// OIDC import client secret for authentication method API.
    /// </summary>
    public class TOidcClientSecretUpPartyController : GenericOAuthClientSecretUpPartyController<OidcUpParty, OidcUpClient>
    {
        public TOidcClientSecretUpPartyController(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository) : base(logger, tenantDataRepository)
        { }

        /// <summary>
        /// Get OIDC client secret for authentication method.
        /// </summary>
        /// <param name="partyName">OIDC authentication method name.</param>
        /// <returns>OIDC client secret for authentication method.</returns>
        [ProducesResponseType(typeof(List<Api.OAuthClientSecretSingleResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OAuthClientSecretSingleResponse>> GetOidcClientSecretUpParty(string partyName) => await Get(partyName);

        /// <summary>
        /// Update OIDC client secret for authentication method.
        /// </summary>
        /// <param name="secretRequest">OIDC client secret for authentication method.</param>
        [ProducesResponseType(typeof(Api.OAuthUpParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OAuthUpParty>> PutOidcClientSecretUpParty([FromBody] Api.OAuthClientSecretSingleRequest secretRequest) => await Put(secretRequest);

        /// <summary>
        /// Delete OIDC client secret for authentication method.
        /// </summary>
        /// <param name="name">Authentication method name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOidcClientSecretUpParty(string name) => await Delete(name);
    }
}
