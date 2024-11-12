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
    /// SAML 2.0 authentication method API.
    /// </summary>
    public class TSamlUpPartyController : GenericPartyApiController<Api.SamlUpParty, Api.SamlClaimTransform, SamlUpParty>
    {
        private readonly ValidateApiModelSamlPartyLogic validateApiModelSamlPartyLogic;
        private readonly SamlMetadataReadUpLogic samlMetadataReadUpLogic;

        public TSamlUpPartyController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, PartyLogic partyLogic, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic, ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic, ValidateModelGenericPartyLogic validateModelGenericPartyLogic, ValidateApiModelSamlPartyLogic validateApiModelSamlPartyLogic, SamlMetadataReadUpLogic samlMetadataReadUpLogic) : base(settings, logger, mapper, tenantDataRepository, partyLogic, downPartyCacheLogic, upPartyCacheLogic, downPartyAllowUpPartiesQueueLogic, validateApiModelGenericPartyLogic, validateModelGenericPartyLogic)
        {
            this.validateApiModelSamlPartyLogic = validateApiModelSamlPartyLogic;
            this.samlMetadataReadUpLogic = samlMetadataReadUpLogic;
        }

        /// <summary>
        /// Get SAML 2.0 authentication method.
        /// </summary>
        /// <param name="name">Authentication method name.</param>
        /// <returns>SAML 2.0 authentication method.</returns>
        [ProducesResponseType(typeof(Api.SamlUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SamlUpParty>> GetSamlUpParty(string name) => await Get(name);

        /// <summary>
        /// Create SAML 2.0 authentication method.
        /// </summary>
        /// <param name="party">SAML 2.0 authentication method.</param>
        /// <returns>SAML 2.0 authentication method.</returns>
        [ProducesResponseType(typeof(Api.SamlUpParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.SamlUpParty>> PostSamlUpParty([FromBody] Api.SamlUpParty party) => await Post(party, ap => new ValueTask<bool>(validateApiModelSamlPartyLogic.ValidateApiModel(ModelState, ap)), async (ap, mp) => await samlMetadataReadUpLogic.PopulateModelAsync(ModelState, mp));

        /// <summary>
        /// Update SAML 2.0 authentication method.
        /// </summary>
        /// <param name="party">SAML 2.0 authentication method.</param>
        /// <returns>SAML 2.0 authentication method.</returns>
        [ProducesResponseType(typeof(Api.SamlUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SamlUpParty>> PutSamlUpParty([FromBody] Api.SamlUpParty party) => await Put(party, ap => new ValueTask<bool>(validateApiModelSamlPartyLogic.ValidateApiModel(ModelState, ap)), async (ap, mp) => await samlMetadataReadUpLogic.PopulateModelAsync(ModelState, mp));

        /// <summary>
        /// Delete SAML 2.0 authentication method.
        /// </summary>
        /// <param name="name">Authentication method name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSamlUpParty(string name) => await Delete(name);
    }
}
