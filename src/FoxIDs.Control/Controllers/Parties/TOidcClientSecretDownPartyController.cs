using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AutoMapper;
using FoxIDs.Logic;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace FoxIDs.Controllers
{
    /// <summary>
    /// OIDC client secret for application registration API.
    /// </summary>
    public class TOidcClientSecretDownPartyController : GenericOAuthClientSecretDownPartyController<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>
    {
        public TOidcClientSecretDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantRepository, SecretHashLogic secretHashLogic) : base(logger, mapper, tenantRepository, secretHashLogic)
        { }

        /// <summary>
        /// Get OIDC client secrets for application registration.
        /// </summary>
        /// <param name="partyName">OIDC application name.</param>
        /// <returns>OIDC client secrets for application registration.</returns>
        [ProducesResponseType(typeof(List<Api.OAuthClientSecretResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<Api.OAuthClientSecretResponse>>> GetOidcClientSecretDownParty(string partyName) => await Get(partyName);

        /// <summary>
        /// Create OIDC client secret for application registration.
        /// </summary>
        /// <param name="secretRequest">OIDC client secret for application registration.</param>
        [ProducesResponseType(typeof(Api.OidcDownParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OidcDownParty>> PostOidcClientSecretDownParty([FromBody] Api.OAuthClientSecretRequest secretRequest) => await Post(secretRequest);

        /// <summary>
        /// Delete OIDC client secret for application registration.
        /// </summary>
        /// <param name="name">Application name and secret id.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOidcClientSecretDownParty(string name) => await Delete(name);
    }
}
