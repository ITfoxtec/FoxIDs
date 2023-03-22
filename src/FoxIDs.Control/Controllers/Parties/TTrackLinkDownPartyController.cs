using FoxIDs.Infrastructure;
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
    /// Track link down-party API.
    /// </summary>
    public class TTrackLinkDownPartyController : GenericPartyApiController<Api.TrackLinkDownParty, Api.OAuthClaimTransform, TrackLinkDownParty>
    {
        public TTrackLinkDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic, ValidateGenericPartyLogic validateGenericPartyLogic) : base(logger, mapper, tenantRepository, downPartyCacheLogic, upPartyCacheLogic, downPartyAllowUpPartiesQueueLogic, validateGenericPartyLogic)
        { }

        /// <summary>
        /// Get track link down-party.
        /// </summary>
        /// <param name="name">Party name.</param>
        /// <returns>Track link down-party.</returns>
        [ProducesResponseType(typeof(Api.TrackLinkDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackLinkDownParty>> GetTrackLinkDownParty(string name) => await Get(name);

        /// <summary>
        /// Create track link down-party.
        /// </summary>
        /// <param name="party">Track link down-party.</param>
        /// <returns>Track link down-party.</returns>
        [ProducesResponseType(typeof(Api.TrackLinkDownParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.TrackLinkDownParty>> PostTrackLinkDownParty([FromBody] Api.TrackLinkDownParty party) => await Post(party);

        /// <summary>
        /// Update track link down-party.
        /// </summary>
        /// <param name="party">Track link down-party.</param>
        /// <returns>OIDC down-party.</returns>
        [ProducesResponseType(typeof(Api.TrackLinkDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackLinkDownParty>> PutTrackLinkDownParty([FromBody] Api.TrackLinkDownParty party) => await Put(party);

        /// <summary>
        /// Delete track link down-party.
        /// </summary>
        /// <param name="name">Party name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTrackLinkDownParty(string name) => await Delete(name);
    }
}
