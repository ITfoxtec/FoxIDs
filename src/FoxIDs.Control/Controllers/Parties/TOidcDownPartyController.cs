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
    /// OpenID Connect application registration API.
    /// </summary>
    public class TOidcDownPartyController : GenericPartyApiController<Api.OidcDownParty, Api.OAuthClaimTransform, OidcDownParty>
    {
        private readonly ValidateApiModelOAuthOidcPartyLogic validateApiModelOAuthOidcPartyLogic;

        public TOidcDownPartyController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, PartyLogic partyLogic, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic, ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic, ValidateModelGenericPartyLogic validateModelGenericPartyLogic, ValidateApiModelOAuthOidcPartyLogic validateApiModelOAuthOidcPartyLogic) : base(settings, logger, mapper, tenantDataRepository, partyLogic, downPartyCacheLogic, upPartyCacheLogic, downPartyAllowUpPartiesQueueLogic, validateApiModelGenericPartyLogic, validateModelGenericPartyLogic)
        {
            this.validateApiModelOAuthOidcPartyLogic = validateApiModelOAuthOidcPartyLogic;
        }

        /// <summary>
        /// Get OIDC application registration.
        /// </summary>
        /// <param name="name">Application name.</param>
        /// <returns>OIDC application registration.</returns>
        [ProducesResponseType(typeof(Api.OidcDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OidcDownParty>> GetOidcDownParty(string name) => await Get(name);

        /// <summary>
        /// Create OIDC application registration.
        /// </summary>
        /// <param name="party">OIDC application registration.</param>
        /// <returns>OIDC application registration.</returns>
        [ProducesResponseType(typeof(Api.OidcDownParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OidcDownParty>> PostOidcDownParty([FromBody] Api.OidcDownParty party) => await Post(party, ap => new ValueTask<bool>(validateApiModelOAuthOidcPartyLogic.ValidateApiModel(ModelState, ap)), async (ap, mp) => await validateApiModelOAuthOidcPartyLogic.ValidateModelAsync(ModelState, mp));

        /// <summary>
        /// Update OIDC application registration.
        /// </summary>
        /// <param name="party">OIDC application registration.</param>
        /// <returns>OIDC application registration.</returns>
        [ProducesResponseType(typeof(Api.OidcDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OidcDownParty>> PutOidcDownParty([FromBody] Api.OidcDownParty party) => await Put(party, ap => new ValueTask<bool>(validateApiModelOAuthOidcPartyLogic.ValidateApiModel(ModelState, ap)), async (ap, mp) => await validateApiModelOAuthOidcPartyLogic.ValidateModelAsync(ModelState, mp));

        /// <summary>
        /// Delete OIDC application registration.
        /// </summary>
        /// <param name="name">Application name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOidcDownParty(string name) => await Delete(name);
    }
}
