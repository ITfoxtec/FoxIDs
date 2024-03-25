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
    /// Environment Link application registration API.
    /// </summary>
    public class TTrackLinkDownPartyController : GenericPartyApiController<Api.TrackLinkDownParty, Api.OAuthClaimTransform, TrackLinkDownParty>
    {
        public TTrackLinkDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic, ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic, ValidateModelGenericPartyLogic validateModelGenericPartyLogic) : base(logger, mapper, tenantDataRepository, downPartyCacheLogic, upPartyCacheLogic, downPartyAllowUpPartiesQueueLogic, validateApiModelGenericPartyLogic, validateModelGenericPartyLogic)
        { }

        /// <summary>
        /// Get environment link application registration.
        /// </summary>
        /// <param name="name">Application name.</param>
        /// <returns>Environment Link application registration.</returns>
        [ProducesResponseType(typeof(Api.TrackLinkDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackLinkDownParty>> GetTrackLinkDownParty(string name) => await Get(name);

        /// <summary>
        /// Create environment link application registration.
        /// </summary>
        /// <param name="party">Environment Link application registration.</param>
        /// <returns>Environment Link application registration.</returns>
        [ProducesResponseType(typeof(Api.TrackLinkDownParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.TrackLinkDownParty>> PostTrackLinkDownParty([FromBody] Api.TrackLinkDownParty party) => await Post(party);

        /// <summary>
        /// Update environment link application registration.
        /// </summary>
        /// <param name="party">Environment Link application registration.</param>
        /// <returns>OIDC application registration.</returns>
        [ProducesResponseType(typeof(Api.TrackLinkDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackLinkDownParty>> PutTrackLinkDownParty([FromBody] Api.TrackLinkDownParty party) => await Put(party);

        /// <summary>
        /// Delete environment link application registration.
        /// </summary>
        /// <param name="name">Application name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTrackLinkDownParty(string name) => await Delete(name);
    }
}
