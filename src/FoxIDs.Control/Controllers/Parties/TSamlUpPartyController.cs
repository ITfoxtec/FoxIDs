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
    /// SAML 2.0 up-party API.
    /// </summary>
    public class TSamlUpPartyController : GenericPartyApiController<Api.SamlUpParty, Api.SamlClaimTransform, SamlUpParty>
    {
        private readonly ValidateSamlPartyLogic validateSamlPartyLogic;
        private readonly SamlMetadataReadUpLogic samlMetadataReadUpLogic;

        public TSamlUpPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic, ValidateGenericPartyLogic validateGenericPartyLogic, ValidateSamlPartyLogic validateSamlPartyLogic, SamlMetadataReadUpLogic samlMetadataReadUpLogic) : base(logger, mapper, tenantRepository, downPartyCacheLogic, upPartyCacheLogic, downPartyAllowUpPartiesQueueLogic, validateGenericPartyLogic)
        {
            this.validateSamlPartyLogic = validateSamlPartyLogic;
            this.samlMetadataReadUpLogic = samlMetadataReadUpLogic;
        }

        /// <summary>
        /// Get SAML 2.0 up-party.
        /// </summary>
        /// <param name="name">Party name.</param>
        /// <returns>SAML 2.0 up-party.</returns>
        [ProducesResponseType(typeof(Api.SamlUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SamlUpParty>> GetSamlUpParty(string name) => await Get(name);

        /// <summary>
        /// Create SAML 2.0 up-party.
        /// </summary>
        /// <param name="party">SAML 2.0 up-party.</param>
        /// <returns>SAML 2.0 up-party.</returns>
        [ProducesResponseType(typeof(Api.SamlUpParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.SamlUpParty>> PostSamlUpParty([FromBody] Api.SamlUpParty party) => await Post(party, ap => new ValueTask<bool>(validateSamlPartyLogic.ValidateApiModel(ModelState, ap)), async (ap, mp) => await samlMetadataReadUpLogic.PopulateModelAsync(ModelState, mp));

        /// <summary>
        /// Update SAML 2.0 up-party.
        /// </summary>
        /// <param name="party">SAML 2.0 up-party.</param>
        /// <returns>SAML 2.0 up-party.</returns>
        [ProducesResponseType(typeof(Api.SamlUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SamlUpParty>> PutSamlUpParty([FromBody] Api.SamlUpParty party) => await Put(party, ap => new ValueTask<bool>(validateSamlPartyLogic.ValidateApiModel(ModelState, ap)), async (ap, mp) => await samlMetadataReadUpLogic.PopulateModelAsync(ModelState, mp));

        /// <summary>
        /// Delete SAML 2.0 up-party.
        /// </summary>
        /// <param name="name">Party name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSamlUpParty(string name) => await Delete(name);
    }
}
