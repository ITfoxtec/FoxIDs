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
    /// OAuth 2.0 client secret for down-party api.
    /// </summary>
    public class TOAuthClientSecretDownPartyController : GenericClientSecretDownPartyController<OAuthDownParty, OAuthDownClient, OAuthDownScope, OAuthDownClaim>
    {
        public TOAuthClientSecretDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, SecretHashLogic secretHashLogic) : base(logger, mapper, tenantRepository, secretHashLogic)
        { }

        /// <summary>
        /// Get OAuth 2.0 client secrets for down-party.
        /// </summary>
        /// <param name="partyName">OAuth 2.0 party name.</param>
        /// <returns>OAuth 2.0 client secrets for down-party.</returns>
        [ProducesResponseType(typeof(List<Api.OAuthClientSecretResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<Api.OAuthClientSecretResponse>>> GetOAuthClientSecretDownParty(string partyName) => await Get(partyName);

        /// <summary>
        /// Create OAuth 2.0 client secret for down-party.
        /// </summary>
        /// <param name="secretRequest">OAuth 2.0 client secret for down-party.</param>
        [ProducesResponseType(typeof(Api.OAuthDownParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OAuthDownParty>> PostOAuthClientSecretDownParty([FromBody] Api.OAuthClientSecretRequest secretRequest) => await Post(secretRequest);

        /// <summary>
        /// Delete OAuth 2.0 client secret for down-party.
        /// </summary>
        /// <param name="name">Party name and secret id.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOAuthClientSecretDownParty(string name) => await Delete(name);
    }
}
