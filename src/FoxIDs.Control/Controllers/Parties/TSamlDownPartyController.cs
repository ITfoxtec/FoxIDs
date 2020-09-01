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
    /// Saml down party api.
    /// </summary>
    public class TSamlDownPartyController : GenericPartyApiController<Api.SamlDownParty, SamlDownParty>
    {
        private readonly ValidateSamlPartyLogic validateSamlPartyLogic;

        public TSamlDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, ValidatePartyLogic validatePartyLogic, ValidateSamlPartyLogic validateSamlPartyLogic) : base(logger, mapper, tenantRepository, validatePartyLogic)
        {
            this.validateSamlPartyLogic = validateSamlPartyLogic;
        }

        /// <summary>
        /// Get Saml down party.
        /// </summary>
        /// <param name="name">Party name.</param>
        /// <returns>Saml down party.</returns>
        [ProducesResponseType(typeof(Api.SamlDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SamlDownParty>> GetSamlDownParty(string name) => await Get(name);

        /// <summary>
        /// Create Saml down party.
        /// </summary>
        /// <param name="party">Saml down party.</param>
        /// <returns>Saml down party.</returns>
        [ProducesResponseType(typeof(Api.SamlDownParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.SamlDownParty>> PostSamlDownParty([FromBody] Api.SamlDownParty party) => await Post(party, ap => Task.FromResult(validateSamlPartyLogic.ValidateApiModel(ModelState, ap)),  (ap, mp) => Task.FromResult(true));

        /// <summary>
        /// Downdate Saml down party.
        /// </summary>
        /// <param name="party">Saml down party.</param>
        /// <returns>Saml down party.</returns>
        [ProducesResponseType(typeof(Api.SamlDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SamlDownParty>> PutSamlDownParty([FromBody] Api.SamlDownParty party) => await Put(party, ap => Task.FromResult(validateSamlPartyLogic.ValidateApiModel(ModelState, ap)), (ap, mp) => Task.FromResult(true));

        /// <summary>
        /// Delete Saml down party.
        /// </summary>
        /// <param name="name">Party name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSamlDownParty(string name) => await Delete(name);
    }
}
