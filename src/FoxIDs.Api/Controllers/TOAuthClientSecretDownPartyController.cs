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
    /// OAuth 2.0 client secret for down party api.
    /// </summary>
    public class TOAuthClientSecretDownPartyController : TenantClientSecretDownPartyController<OAuthDownParty, OAuthDownClient, OAuthDownScope, OAuthDownClaim>
    {
        public TOAuthClientSecretDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantService, SecretHashLogic secretHashLogic) : base(logger, mapper, tenantService, secretHashLogic)
        { }

        /// <summary>
        /// Get OAuth 2.0 client secrets for down party.
        /// </summary>
        /// <param name="partyName">OAuth 2.0 party name.</param>
        /// <returns>OAuth 2.0 client secrets for down party.</returns>
        [ProducesResponseType(typeof(Api.OAuthClientSecretResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OAuthClientSecretResponse>> GetOAuthClientSecretDownParty(string partyName) => await Get(partyName);

        /// <summary>
        /// Create OAuth 2.0 client secret for down party.
        /// </summary>
        /// <param name="party">OAuth 2.0 client secret for down party.</param>
        /// <returns>OAuth 2.0 client secret for down party.</returns>
        [ProducesResponseType(typeof(Api.OAuthClientSecretResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OAuthClientSecretResponse>> PostOAuthClientSecretDownParty([FromBody] Api.OAuthClientSecretRequest party) => await Post(party);

        /// <summary>
        /// Delete OAuth 2.0 client secret for down party.
        /// </summary>
        /// <param name="name">Party name and secret id.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOAuthClientSecretDownParty(string name) => await Delete(name);
    }
}
