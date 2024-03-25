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
    /// OAuth authentication method API.
    /// </summary>
    public class TOAuthUpPartyController : GenericPartyApiController<Api.OAuthUpParty, Api.OAuthClaimTransform, OAuthUpParty>
    {
        private readonly ValidateApiModelOAuthOidcPartyLogic validateApiModelOAuthOidcPartyLogic;
        private readonly OidcDiscoveryReadUpLogic<OAuthUpParty, OAuthUpClient> oidchDiscoveryReadUpLogic;

        public TOAuthUpPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantRepository, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic, ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic, ValidateModelGenericPartyLogic validateModelGenericPartyLogic, ValidateApiModelOAuthOidcPartyLogic validateApiModelOAuthOidcPartyLogic, OidcDiscoveryReadUpLogic<OAuthUpParty, OAuthUpClient> oidcDiscoveryReadUpLogic) : base(logger, mapper, tenantRepository, downPartyCacheLogic, upPartyCacheLogic, downPartyAllowUpPartiesQueueLogic, validateApiModelGenericPartyLogic, validateModelGenericPartyLogic)
        {
            this.validateApiModelOAuthOidcPartyLogic = validateApiModelOAuthOidcPartyLogic;
            this.oidchDiscoveryReadUpLogic = oidcDiscoveryReadUpLogic;
        }

        /// <summary>
        /// Get OAuth authentication method.
        /// </summary>
        /// <param name="name">Authentication method name.</param>
        /// <returns>OAuth authentication method.</returns>
        [ProducesResponseType(typeof(Api.OAuthUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OAuthUpParty>> GetOAuthUpParty(string name) => await Get(name);

        /// <summary>
        /// Create OAuth authentication method.
        /// </summary>
        /// <param name="party">OAuth authentication method.</param>
        /// <returns>OAuth authentication method.</returns>
        [ProducesResponseType(typeof(Api.OAuthUpParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OAuthUpParty>> PostOAuthUpParty([FromBody] Api.OAuthUpParty party) => await Post(party, ap => new ValueTask<bool>(validateApiModelOAuthOidcPartyLogic.ValidateApiModel(ModelState, ap)), async (ap, mp) => await oidchDiscoveryReadUpLogic.PopulateModelAsync(ModelState, mp));

        /// <summary>
        /// Update OAuth authentication method.
        /// </summary>
        /// <param name="party">OAuth authentication method.</param>
        /// <returns>OAuth authentication method.</returns>
        [ProducesResponseType(typeof(Api.OAuthUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OAuthUpParty>> PutOAuthUpParty([FromBody] Api.OAuthUpParty party) => await Put(party, ap => new ValueTask<bool>(validateApiModelOAuthOidcPartyLogic.ValidateApiModel(ModelState, ap)), async (ap, mp) => await oidchDiscoveryReadUpLogic.PopulateModelAsync(ModelState, mp));

        /// <summary>
        /// Delete OAuth authentication method.
        /// </summary>
        /// <param name="name">Authentication method name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOAuthUpParty(string name) => await Delete(name);
    }
}
