using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AutoMapper;
using FoxIDs.Logic;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Controllers
{
    /// <summary>
    /// OIDC client secret for down party api.
    /// </summary>
    public class TOidcClientSecretDownPartyController : TenantClientSecretDownPartyController<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>
    {
        public TOidcClientSecretDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantService, SecretHashLogic secretHashLogic) : base(logger, mapper, tenantService, secretHashLogic)
        { }

        /// <summary>
        /// Get OIDC client secrets for down party.
        /// </summary>
        /// <param name="partyName">OIDC party name.</param>
        /// <returns>OIDC client secrets for down party.</returns>
        [ProducesResponseType(typeof(Api.OAuthClientSecretResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OAuthClientSecretResponse>> GetOidcClientSecretDownParty(string partyName) => await Get(partyName);

        /// <summary>
        /// Create OIDC client secret for down party.
        /// </summary>
        /// <param name="party">OIDC client secret for down party.</param>
        /// <returns>OIDC client secret for down party.</returns>
        [ProducesResponseType(typeof(Api.OAuthClientSecretResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OAuthClientSecretResponse>> PostOidcClientSecretDownParty([FromBody] Api.OAuthClientSecretRequest party) => await Post(party);

        /// <summary>
        /// Delete OIDC client secret for down party.
        /// </summary>
        /// <param name="name">Party name and secret id.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOidcClientSecretDownParty(string name) => await Delete(name);
    }
}
