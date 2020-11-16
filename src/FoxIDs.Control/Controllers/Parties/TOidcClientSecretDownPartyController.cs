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
    /// OIDC client secret for down-party API.
    /// </summary>
    public class TOidcClientSecretDownPartyController : GenericClientSecretDownPartyController<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>
    {
        public TOidcClientSecretDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, SecretHashLogic secretHashLogic) : base(logger, mapper, tenantRepository, secretHashLogic)
        { }

        /// <summary>
        /// Get OIDC client secrets for down-party.
        /// </summary>
        /// <param name="partyName">OIDC party name.</param>
        /// <returns>OIDC client secrets for down-party.</returns>
        [ProducesResponseType(typeof(List<Api.OAuthClientSecretResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<Api.OAuthClientSecretResponse>>> GetOidcClientSecretDownParty(string partyName) => await Get(partyName);

        /// <summary>
        /// Create OIDC client secret for down-party.
        /// </summary>
        /// <param name="secretRequest">OIDC client secret for down-party.</param>
        [ProducesResponseType(typeof(Api.OidcDownParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OidcDownParty>> PostOidcClientSecretDownParty([FromBody] Api.OAuthClientSecretRequest secretRequest) => await Post(secretRequest);

        /// <summary>
        /// Delete OIDC client secret for down-party.
        /// </summary>
        /// <param name="name">Party name and secret id.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOidcClientSecretDownParty(string name) => await Delete(name);
    }
}
