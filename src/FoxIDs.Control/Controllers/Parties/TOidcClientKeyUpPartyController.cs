﻿using FoxIDs.Infrastructure;
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
    /// OIDC import client key for up-party API.
    /// </summary>
    public class TOidcClientKeyUpPartyController : GenericOAuthClientKeyUpPartyController<OidcUpParty, OidcUpClient>
    {
        public TOidcClientKeyUpPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, PlanCacheLogic planCacheLogic, ExternalKeyLogic externalKeyLogic) : base(logger, mapper, tenantRepository, planCacheLogic, externalKeyLogic)
        { }

        /// <summary>
        /// Get OIDC client key for up-party.
        /// </summary>
        /// <param name="partyName">OIDC party name.</param>
        /// <returns>OIDC client key for up-party.</returns>
        [ProducesResponseType(typeof(List<Api.OAuthClientKeyResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OAuthClientKeyResponse>> GetOidcClientKeyUpParty(string partyName) => await Get(partyName);

        /// <summary>
        /// Create OIDC client key for up-party.
        /// </summary>
        /// <param name="keyRequest">OIDC client key for up-party.</param>
        [ProducesResponseType(typeof(Api.OAuthClientKeyResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OAuthClientKeyResponse>> PostOidcClientKeyUpParty([FromBody] Api.OAuthClientKeyRequest keyRequest) => await Post(keyRequest);

        /// <summary>
        /// Delete OIDC client key for up-party.
        /// </summary>
        /// <param name="name">Name is [up-party name].[key name] </param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOidcClientKeyUpParty(string name) => await Delete(name);
    }
}
