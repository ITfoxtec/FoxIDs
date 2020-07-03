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
    /// Saml up party api.
    /// </summary>
    public class TSamlUpPartyController : TenantPartyApiController<Api.SamlUpParty, SamlUpParty>
    {
        private readonly ValidateSamlPartyLogic validateSamlPartyLogic;

        public TSamlUpPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantService, ValidatePartyLogic validatePartyLogic, ValidateSamlPartyLogic validateSamlPartyLogic) : base(logger, mapper, tenantService, validatePartyLogic)
        {
            this.validateSamlPartyLogic = validateSamlPartyLogic;
        }

        /// <summary>
        /// Get Saml up party.
        /// </summary>
        /// <param name="name">Party name.</param>
        /// <returns>Saml up party.</returns>
        [ProducesResponseType(typeof(Api.SamlUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SamlUpParty>> GetSamlUpParty(string name) => await Get(name);

        /// <summary>
        /// Create Saml up party.
        /// </summary>
        /// <param name="party">Saml up party.</param>
        /// <returns>Saml up party.</returns>
        [ProducesResponseType(typeof(Api.SamlUpParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.SamlUpParty>> PostSamlUpParty([FromBody] Api.SamlUpParty party) => await Post(party, ap => Task.FromResult(validateSamlPartyLogic.ValidateApiModel(ModelState, ap)),  (ap, mp) => Task.FromResult(true));

        /// <summary>
        /// Update Saml up party.
        /// </summary>
        /// <param name="party">Saml up party.</param>
        /// <returns>Saml up party.</returns>
        [ProducesResponseType(typeof(Api.SamlUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SamlUpParty>> PutSamlUpParty([FromBody] Api.SamlUpParty party) => await Put(party, ap => Task.FromResult(validateSamlPartyLogic.ValidateApiModel(ModelState, ap)), (ap, mp) => Task.FromResult(true));
       
        /// <summary>
        /// Delete Saml up party.
        /// </summary>
        /// <param name="name">Party name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSamlUpParty(string name) => await Delete(name);
    }
}
