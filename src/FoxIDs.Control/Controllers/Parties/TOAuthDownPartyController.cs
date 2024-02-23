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
    /// OAuth 2.0 application registration API.
    /// </summary>
    public class TOAuthDownPartyController : GenericPartyApiController<Api.OAuthDownParty, Api.OAuthClaimTransform, OAuthDownParty>
    {
        private readonly ValidateApiModelOAuthOidcPartyLogic validateApiModelOAuthOidcPartyLogic;

        public TOAuthDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic, ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic, ValidateModelGenericPartyLogic validateModelGenericPartyLogic, ValidateApiModelOAuthOidcPartyLogic validateApiModelOAuthOidcPartyLogic) : base(logger, mapper, tenantRepository, downPartyCacheLogic, upPartyCacheLogic, downPartyAllowUpPartiesQueueLogic, validateApiModelGenericPartyLogic, validateModelGenericPartyLogic)
        {
            this.validateApiModelOAuthOidcPartyLogic = validateApiModelOAuthOidcPartyLogic;
        }

        /// <summary>
        /// Get OAuth 2.0 application registration.
        /// </summary>
        /// <param name="name">Party name.</param>
        /// <returns>OAuth 2.0 application registration.</returns>
        [ProducesResponseType(typeof(Api.OAuthDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OAuthDownParty>> GetOAuthDownParty(string name) => await Get(name);

        /// <summary>
        /// Create OAuth 2.0 application registration.
        /// </summary>
        /// <param name="party">OAuth 2.0 application registration.</param>
        /// <returns>OAuth 2.0 application registration.</returns>
        [ProducesResponseType(typeof(Api.OAuthDownParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OAuthDownParty>> PostOAuthDownParty([FromBody] Api.OAuthDownParty party) => await Post(party, ap => new ValueTask<bool>(validateApiModelOAuthOidcPartyLogic.ValidateApiModel(ModelState, ap)), async (ap, mp) => await validateApiModelOAuthOidcPartyLogic.ValidateModelAsync(ModelState, mp));

        /// <summary>
        /// Update OAuth 2.0 application registration.
        /// </summary>
        /// <param name="party">OAuth 2.0 application registration.</param>
        /// <returns>OAuth 2.0 application registration.</returns>
        [ProducesResponseType(typeof(Api.OAuthDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OAuthDownParty>> PutOAuthDownParty([FromBody] Api.OAuthDownParty party) => await Put(party, ap => new ValueTask<bool>(validateApiModelOAuthOidcPartyLogic.ValidateApiModel(ModelState, ap)), async (ap, mp) => await validateApiModelOAuthOidcPartyLogic.ValidateModelAsync(ModelState, mp));

        /// <summary>
        /// Delete OAuth 2.0 application registration.
        /// </summary>
        /// <param name="name">Party name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOAuthDownParty(string name) => await Delete(name);
    }
}
