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
    /// OIDC import client secret for up-party API.
    /// </summary>
    public class TOidcClientSecretUpPartyController : GenericOAuthClientSecretUpPartyController<OidcUpParty, OidcUpClient>
    {
        public TOidcClientSecretUpPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, PlanCacheLogic planCacheLogic, ExternalKeyLogic externalKeyLogic) : base(logger, mapper, tenantRepository, planCacheLogic, externalKeyLogic)
        { }

        /// <summary>
        /// Get OIDC client secret for up-party.
        /// </summary>
        /// <param name="partyName">OIDC party name.</param>
        /// <returns>OIDC client secret for up-party.</returns>
        [ProducesResponseType(typeof(List<Api.OAuthClientSecretSingleResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OAuthClientSecretSingleResponse>> GetOidcClientSecretUpParty(string partyName) => await Get(partyName);

        /// <summary>
        /// Update OIDC client secret for up-party.
        /// </summary>
        /// <param name="secretRequest">OIDC client secret for up-party.</param>
        [ProducesResponseType(typeof(Api.OAuthUpParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OAuthUpParty>> PutOidcClientSecretUpParty([FromBody] Api.OAuthClientSecretSingleRequest secretRequest) => await Put(secretRequest);

        /// <summary>
        /// Delete OIDC client secret for up-party.
        /// </summary>
        /// <param name="name">Party name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOidcClientSecretUpParty(string name) => await Delete(name);
    }
}
