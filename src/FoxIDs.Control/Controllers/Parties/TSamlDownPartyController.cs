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
    /// SAML 2.0 down-party API.
    /// </summary>
    public class TSamlDownPartyController : GenericPartyApiController<Api.SamlDownParty, Api.SamlClaimTransform, SamlDownParty>
    {
        private readonly ValidateSamlPartyLogic validateSamlPartyLogic;

        public TSamlDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, ValidateGenericPartyLogic validateGenericPartyLogic, ValidateSamlPartyLogic validateSamlPartyLogic) : base(logger, mapper, tenantRepository, downPartyCacheLogic, upPartyCacheLogic, validateGenericPartyLogic)
        {
            this.validateSamlPartyLogic = validateSamlPartyLogic;
        }

        /// <summary>
        /// Get SAML 2.0 down-party.
        /// </summary>
        /// <param name="name">Party name.</param>
        /// <returns>SAML 2.0 down-party.</returns>
        [ProducesResponseType(typeof(Api.SamlDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SamlDownParty>> GetSamlDownParty(string name) => await Get(name);

        /// <summary>
        /// Create SAML 2.0 down-party.
        /// </summary>
        /// <param name="party">SAML 2.0 down-party.</param>
        /// <returns>SAML 2.0 down-party.</returns>
        [ProducesResponseType(typeof(Api.SamlDownParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.SamlDownParty>> PostSamlDownParty([FromBody] Api.SamlDownParty party) => await Post(party, ap => new ValueTask<bool>(validateSamlPartyLogic.ValidateApiModel(ModelState, ap)),  (ap, mp) => new ValueTask<bool>(true));

        /// <summary>
        /// Update SAML 2.0 down-party.
        /// </summary>
        /// <param name="party">SAML 2.0 down-party.</param>
        /// <returns>SAML 2.0 down-party.</returns>
        [ProducesResponseType(typeof(Api.SamlDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SamlDownParty>> PutSamlDownParty([FromBody] Api.SamlDownParty party) => await Put(party, ap => new ValueTask<bool>(validateSamlPartyLogic.ValidateApiModel(ModelState, ap)), (ap, mp) => new ValueTask<bool>(true));

        /// <summary>
        /// Delete SAML 2.0 down-party.
        /// </summary>
        /// <param name="name">Party name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSamlDownParty(string name) => await Delete(name);
    }
}
