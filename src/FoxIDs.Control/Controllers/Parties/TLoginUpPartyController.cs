using FoxIDs.Infrastructure;
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
    /// Login authentication method API.
    /// </summary>
    public class TLoginUpPartyController : GenericPartyApiController<Api.LoginUpParty, Api.OAuthClaimTransform, LoginUpParty>
    {
        private readonly ValidateApiModelLoginPartyLogic validateApiModelLoginPartyLogic;

        public TLoginUpPartyController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, PartyLogic partyLogic, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic, ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic, ValidateModelGenericPartyLogic validateModelGenericPartyLogic, ValidateApiModelLoginPartyLogic validateApiModelLoginPartyLogic) : base(settings, logger, mapper, tenantDataRepository, partyLogic, downPartyCacheLogic, upPartyCacheLogic, downPartyAllowUpPartiesQueueLogic, validateApiModelGenericPartyLogic, validateModelGenericPartyLogic)
        {
            this.validateApiModelLoginPartyLogic = validateApiModelLoginPartyLogic;
        }

        /// <summary>
        /// Get Login authentication method.
        /// </summary>
        /// <param name="name">Authentication method name.</param>
        /// <returns>Login authentication method.</returns>
        [ProducesResponseType(typeof(Api.LoginUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LoginUpParty>> GetLoginUpParty(string name) => await Get(name);

        /// <summary>
        /// Create Login authentication method.
        /// </summary>
        /// <param name="party">Login authentication method.</param>
        /// <returns>Login authentication method.</returns>
        [ProducesResponseType(typeof(Api.LoginUpParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.LoginUpParty>> PostLoginUpParty([FromBody] Api.LoginUpParty party) => await Post(party, async ap => await validateApiModelLoginPartyLogic.ValidateApiModelAsync(ModelState, ap));

        /// <summary>
        /// Update Login authentication method.
        /// </summary>
        /// <param name="party">Login authentication method.</param>
        /// <returns>Login authentication method.</returns>
        [ProducesResponseType(typeof(Api.LoginUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LoginUpParty>> PutLoginUpParty([FromBody] Api.LoginUpParty party) => await Put(party, async ap => await validateApiModelLoginPartyLogic.ValidateApiModelAsync(ModelState, ap));

        /// <summary>
        /// Delete Login authentication method.
        /// </summary>
        /// <param name="name">Authentication method name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteLoginUpParty(string name) => await Delete(name);
    }
}
