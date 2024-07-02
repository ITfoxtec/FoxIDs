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

namespace FoxIDs.Controllers
{
    /// <summary>
    /// External login authentication method API.
    /// </summary>
    public class TExternalLoginUpPartyController : GenericPartyApiController<Api.ExternalLoginUpParty, Api.OAuthClaimTransform, ExternalLoginUpParty>
    {
        private readonly ValidateApiModelExternalLoginPartyLogic validateApiModelExternalLoginPartyLogic;

        public TExternalLoginUpPartyController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, PartyLogic partyLogic, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic, ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic, ValidateModelGenericPartyLogic validateModelGenericPartyLogic, ValidateApiModelExternalLoginPartyLogic validateApiModelExternalLoginPartyLogic) : base(settings, logger, mapper, tenantDataRepository, partyLogic, downPartyCacheLogic, upPartyCacheLogic, downPartyAllowUpPartiesQueueLogic, validateApiModelGenericPartyLogic, validateModelGenericPartyLogic)
        {
            this.validateApiModelExternalLoginPartyLogic = validateApiModelExternalLoginPartyLogic;
        }

        /// <summary>
        /// Get external login authentication method.
        /// </summary>
        /// <param name="name">Authentication method name.</param>
        /// <returns>External login authentication method.</returns>
        [ProducesResponseType(typeof(Api.ExternalLoginUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.ExternalLoginUpParty>> GetExternalloginUpParty(string name) => await Get(name);

        /// <summary>
        /// Create external login authentication method.
        /// </summary>
        /// <param name="party">API authentication method.</param>
        /// <returns>External login authentication method.</returns>
        [ProducesResponseType(typeof(Api.ExternalLoginUpParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.ExternalLoginUpParty>> PostExternalloginUpParty([FromBody] Api.ExternalLoginUpParty party) => await Post(party, ap => new ValueTask<bool>(validateApiModelExternalLoginPartyLogic.ValidateApiModel(ModelState, ap)));

        /// <summary>
        /// Update external login authentication method.
        /// </summary>
        /// <param name="party">API authentication method.</param>
        /// <returns>External login authentication method.</returns>
        [ProducesResponseType(typeof(Api.ExternalLoginUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.ExternalLoginUpParty>> PutExternalloginUpParty([FromBody] Api.ExternalLoginUpParty party) => await Put(party, ap => new ValueTask<bool>(validateApiModelExternalLoginPartyLogic.ValidateApiModel(ModelState, ap)));

        /// <summary>
        /// Delete external login authentication method.
        /// </summary>
        /// <param name="name">Authentication method name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteExternalloginUpParty(string name) => await Delete(name);
    }
}
