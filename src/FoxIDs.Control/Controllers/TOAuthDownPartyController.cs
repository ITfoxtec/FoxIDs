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
    /// OAuth 2.0 down party api.
    /// </summary>
    public class TOAuthDownPartyController : TenantPartyApiController<Api.OAuthDownParty, OAuthDownParty>
    {
        private readonly ValidateOAuthOidcLogic validateOAuthOidcLogic;

        public TOAuthDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantService, ValidatePartyLogic validatePartyLogic, ValidateOAuthOidcLogic validateOAuthOidcLogic) : base(logger, mapper, tenantService, validatePartyLogic)
        {
            this.validateOAuthOidcLogic = validateOAuthOidcLogic;
        }

        /// <summary>
        /// Get OAuth 2.0 down party.
        /// </summary>
        /// <param name="name">Party name.</param>
        /// <returns>OAuth 2.0 down party.</returns>
        [ProducesResponseType(typeof(Api.OAuthDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OAuthDownParty>> GetOAuthDownParty(string name) => await Get(name);

        /// <summary>
        /// Create OAuth 2.0 down party.
        /// </summary>
        /// <param name="party">OAuth 2.0 down party.</param>
        /// <returns>OAuth 2.0 down party.</returns>
        [ProducesResponseType(typeof(Api.OAuthDownParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OAuthDownParty>> PostOAuthDownParty([FromBody] Api.OAuthDownParty party) => await Post(party, ap => Task.FromResult(true), async (ap, mp) => await validateOAuthOidcLogic.ValidateModelAsync(ModelState, mp));

        /// <summary>
        /// Update OAuth 2.0 down party.
        /// </summary>
        /// <param name="party">OAuth 2.0 down party.</param>
        /// <returns>OAuth 2.0 down party.</returns>
        [ProducesResponseType(typeof(Api.OAuthDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OAuthDownParty>> PutOAuthDownParty([FromBody] Api.OAuthDownParty party) => await Put(party, ap => Task.FromResult(true), async (ap, mp) => await validateOAuthOidcLogic.ValidateModelAsync(ModelState, mp));

        /// <summary>
        /// Delete OAuth 2.0 down party.
        /// </summary>
        /// <param name="name">Party name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOAuthDownParty(string name) => await Delete(name);
    }
}
