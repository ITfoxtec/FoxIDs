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
    /// Environment Link authentication method API.
    /// </summary>
    public class TTrackLinkUpPartyController : GenericPartyApiController<Api.TrackLinkUpParty, Api.OAuthClaimTransform, TrackLinkUpParty>
    {
        public TTrackLinkUpPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic, ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic, ValidateModelGenericPartyLogic validateModelGenericPartyLogic) : base(logger, mapper, tenantRepository, downPartyCacheLogic, upPartyCacheLogic, downPartyAllowUpPartiesQueueLogic, validateApiModelGenericPartyLogic, validateModelGenericPartyLogic)
        { }

        /// <summary>
        /// Get environment link authentication method.
        /// </summary>
        /// <param name="name">Authentication method name.</param>
        /// <returns>Environment Link authentication method.</returns>
        [ProducesResponseType(typeof(Api.TrackLinkUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackLinkUpParty>> GetTrackLinkUpParty(string name) => await Get(name);

        /// <summary>
        /// Create environment link authentication method.
        /// </summary>
        /// <param name="party">Environment Link authentication method.</param>
        /// <returns>Environment Link authentication method.</returns>
        [ProducesResponseType(typeof(Api.TrackLinkUpParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.TrackLinkUpParty>> PostTrackLinkUpParty([FromBody] Api.TrackLinkUpParty party) => await Post(party);

        /// <summary>
        /// Update environment link authentication method.
        /// </summary>
        /// <param name="party">Environment Link authentication method.</param>
        /// <returns>Environment Link authentication method.</returns>
        [ProducesResponseType(typeof(Api.TrackLinkUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackLinkUpParty>> PutTrackLinkUpParty([FromBody] Api.TrackLinkUpParty party) => await Put(party);

        /// <summary>
        /// Delete environment link authentication method.
        /// </summary>
        /// <param name="name">Authentication method name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTrackLinkUpParty(string name) => await Delete(name);
    }
}
