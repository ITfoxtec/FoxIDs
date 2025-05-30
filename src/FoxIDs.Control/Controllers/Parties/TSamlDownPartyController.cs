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
    /// SAML 2.0 application registration API.
    /// </summary>
    public class TSamlDownPartyController : GenericPartyApiController<Api.SamlDownParty, Api.SamlClaimTransform, SamlDownParty>
    {
        private readonly ValidateApiModelSamlPartyLogic validateApiModelSamlPartyLogic;

        public TSamlDownPartyController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, PartyLogic partyLogic, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic, ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic, ValidateModelGenericPartyLogic validateModelGenericPartyLogic, ValidateApiModelSamlPartyLogic validateApiModelSamlPartyLogic) : base(settings, logger, mapper, tenantDataRepository, partyLogic, downPartyCacheLogic, upPartyCacheLogic, downPartyAllowUpPartiesQueueLogic, validateApiModelGenericPartyLogic, validateModelGenericPartyLogic)
        {
            this.validateApiModelSamlPartyLogic = validateApiModelSamlPartyLogic;
        }

        /// <summary>
        /// Get SAML 2.0 application registration.
        /// </summary>
        /// <param name="name">Application name.</param>
        /// <returns>SAML 2.0 application registration.</returns>
        [ProducesResponseType(typeof(Api.SamlDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SamlDownParty>> GetSamlDownParty(string name) => await Get(name);

        /// <summary>
        /// Create SAML 2.0 application registration.
        /// </summary>
        /// <param name="party">SAML 2.0 application registration.</param>
        /// <returns>SAML 2.0 application registration.</returns>
        [ProducesResponseType(typeof(Api.SamlDownParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.SamlDownParty>> PostSamlDownParty([FromBody] Api.SamlDownParty party) => await Post(party, ap => new ValueTask<bool>(validateApiModelSamlPartyLogic.ValidateApiModel(ModelState, ap)));

        /// <summary>
        /// Update SAML 2.0 application registration.
        /// </summary>
        /// <param name="party">SAML 2.0 application registration.</param>
        /// <returns>SAML 2.0 application registration.</returns>
        [ProducesResponseType(typeof(Api.SamlDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SamlDownParty>> PutSamlDownParty([FromBody] Api.SamlDownParty party) => await Put(party, ap => new ValueTask<bool>(validateApiModelSamlPartyLogic.ValidateApiModel(ModelState, ap)));

        /// <summary>
        /// Delete SAML 2.0 application registration.
        /// </summary>
        /// <param name="name">Application name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSamlDownParty(string name) => await Delete(name);
    }
}
