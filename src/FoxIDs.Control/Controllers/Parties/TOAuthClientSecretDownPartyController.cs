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
    /// OAuth 2.0 client secret for application registration API.
    /// </summary>
    public class TOAuthClientSecretDownPartyController : GenericOAuthClientSecretDownPartyController<OAuthDownParty, OAuthDownClient, OAuthDownScope, OAuthDownClaim>
    {
        public TOAuthClientSecretDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, SecretHashLogic secretHashLogic) : base(logger, mapper, tenantDataRepository, secretHashLogic)
        { }

        /// <summary>
        /// Get OAuth 2.0 client secrets for application registration.
        /// </summary>
        /// <param name="partyName">OAuth 2.0 party name.</param>
        /// <returns>OAuth 2.0 client secrets for application registration.</returns>
        [ProducesResponseType(typeof(List<Api.OAuthClientSecretResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<Api.OAuthClientSecretResponse>>> GetOAuthClientSecretDownParty(string partyName) => await Get(partyName);

        /// <summary>
        /// Create OAuth 2.0 client secret for application registration.
        /// </summary>
        /// <param name="secretRequest">OAuth 2.0 client secret for application registration.</param>
        [ProducesResponseType(typeof(Api.OAuthDownParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OAuthDownParty>> PostOAuthClientSecretDownParty([FromBody] Api.OAuthClientSecretRequest secretRequest) => await Post(secretRequest);

        /// <summary>
        /// Delete OAuth 2.0 client secret for application registration.
        /// </summary>
        /// <param name="name">Application name and secret id.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOAuthClientSecretDownParty(string name) => await Delete(name);
    }
}
