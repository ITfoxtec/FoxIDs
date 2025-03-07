﻿using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AutoMapper;
using FoxIDs.Logic;
using FoxIDs.Models.Config;
using FoxIDs.Logic.Queues;

namespace FoxIDs.Controllers
{
    /// <summary>
    /// OIDC authentication method API.
    /// </summary>
    public class TOidcUpPartyController : GenericPartyApiController<Api.OidcUpParty, Api.OAuthClaimTransform, OidcUpParty>
    {
        private readonly ValidateApiModelOAuthOidcPartyLogic validateApiModelOAuthOidcPartyLogic;
        private readonly ValidateModelOAuthOidcPartyLogic validateModelOAuthOidcPartyLogic;
        private readonly OidcDiscoveryReadUpLogic<OidcUpParty, OidcUpClient> oidcDiscoveryReadUpLogic;

        public TOidcUpPartyController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, PartyLogic partyLogic, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic, ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic, ValidateModelGenericPartyLogic validateModelGenericPartyLogic, ValidateApiModelOAuthOidcPartyLogic validateApiModelOAuthOidcPartyLogic, ValidateModelOAuthOidcPartyLogic validateModelOAuthOidcPartyLogic, OidcDiscoveryReadUpLogic<OidcUpParty, OidcUpClient> oidcDiscoveryReadUpLogic) : base(settings, logger, mapper, tenantDataRepository, partyLogic, downPartyCacheLogic, upPartyCacheLogic, downPartyAllowUpPartiesQueueLogic, validateApiModelGenericPartyLogic, validateModelGenericPartyLogic)
        {
            this.validateApiModelOAuthOidcPartyLogic = validateApiModelOAuthOidcPartyLogic;
            this.validateModelOAuthOidcPartyLogic = validateModelOAuthOidcPartyLogic;
            this.oidcDiscoveryReadUpLogic = oidcDiscoveryReadUpLogic;
        }

        /// <summary>
        /// Get OIDC authentication method.
        /// </summary>
        /// <param name="name">Authentication method name.</param>
        /// <returns>OIDC authentication method.</returns>
        [ProducesResponseType(typeof(Api.OidcUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OidcUpParty>> GetOidcUpParty(string name) => await Get(name);

        /// <summary>
        /// Create OIDC authentication method.
        /// </summary>
        /// <param name="party">OIDC authentication method.</param>
        /// <returns>OIDC authentication method.</returns>
        [ProducesResponseType(typeof(Api.OidcUpParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OidcUpParty>> PostOidcUpParty([FromBody] Api.OidcUpParty party) => await Post(party, ap => new ValueTask<bool>(validateApiModelOAuthOidcPartyLogic.ValidateApiModel(ModelState, ap)), async (ap, mp) => await oidcDiscoveryReadUpLogic.PopulateModelAsync(ModelState, mp), (ap, mp) => new ValueTask<bool>(validateModelOAuthOidcPartyLogic.ValidateModel(ModelState, mp)));

        /// <summary>
        /// Update OIDC authentication method.
        /// You cannot update the ClientSecret in this method.
        /// </summary>
        /// <param name="party">OIDC authentication method.</param>
        /// <returns>OIDC authentication method.</returns>
        [ProducesResponseType(typeof(Api.OidcUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OidcUpParty>> PutOidcUpParty([FromBody] Api.OidcUpParty party) => await Put(party, ap => new ValueTask<bool>(validateApiModelOAuthOidcPartyLogic.ValidateApiModel(ModelState, ap)), async (ap, mp) => await oidcDiscoveryReadUpLogic.PopulateModelAsync(ModelState, mp), (ap, mp) => new ValueTask<bool>(validateModelOAuthOidcPartyLogic.ValidateModel(ModelState, mp)));

        /// <summary>
        /// Delete OIDC authentication method.
        /// </summary>
        /// <param name="name">Authentication method name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOidcUpParty(string name) => await Delete(name);
    }
}
