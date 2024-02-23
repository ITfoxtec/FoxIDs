﻿using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AutoMapper;
using FoxIDs.Logic;

namespace FoxIDs.Controllers
{
    /// <summary>
    /// Environment Link application registration API.
    /// </summary>
    public class TTrackLinkDownPartyController : GenericPartyApiController<Api.TrackLinkDownParty, Api.OAuthClaimTransform, TrackLinkDownParty>
    {
        public TTrackLinkDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic, ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic, ValidateModelGenericPartyLogic validateModelGenericPartyLogic) : base(logger, mapper, tenantRepository, downPartyCacheLogic, upPartyCacheLogic, downPartyAllowUpPartiesQueueLogic, validateApiModelGenericPartyLogic, validateModelGenericPartyLogic)
        { }

        /// <summary>
        /// Get environment link application registration.
        /// </summary>
        /// <param name="name">Party name.</param>
        /// <returns>Track link application registration.</returns>
        [ProducesResponseType(typeof(Api.TrackLinkDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackLinkDownParty>> GetTrackLinkDownParty(string name) => await Get(name);

        /// <summary>
        /// Create environment link application registration.
        /// </summary>
        /// <param name="party">Track link application registration.</param>
        /// <returns>Track link application registration.</returns>
        [ProducesResponseType(typeof(Api.TrackLinkDownParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.TrackLinkDownParty>> PostTrackLinkDownParty([FromBody] Api.TrackLinkDownParty party) => await Post(party);

        /// <summary>
        /// Update environment link application registration.
        /// </summary>
        /// <param name="party">Track link application registration.</param>
        /// <returns>OIDC application registration.</returns>
        [ProducesResponseType(typeof(Api.TrackLinkDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackLinkDownParty>> PutTrackLinkDownParty([FromBody] Api.TrackLinkDownParty party) => await Put(party);

        /// <summary>
        /// Delete environment link application registration.
        /// </summary>
        /// <param name="name">Party name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTrackLinkDownParty(string name) => await Delete(name);
    }
}
