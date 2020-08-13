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
    /// OpenID Connect down party api.
    /// </summary>
    public class TOidcDownPartyController : TenantPartyApiController<Api.OidcDownParty, OidcDownParty>
    {
        private readonly ValidateOAuthOidcLogic validateOAuthOidcLogic;

        public TOidcDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantService, ValidatePartyLogic validatePartyLogic, ValidateOAuthOidcLogic validateOAuthOidcLogic) : base(logger, mapper, tenantService, validatePartyLogic)
        {
            this.validateOAuthOidcLogic = validateOAuthOidcLogic;
        }

        /// <summary>
        /// Get OIDC down party.
        /// </summary>
        /// <param name="name">Party name.</param>
        /// <returns>OIDC down party.</returns>
        [ProducesResponseType(typeof(Api.OidcDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OidcDownParty>> GetOidcDownParty(string name) => await Get(name);

        /// <summary>
        /// Create OIDC down party.
        /// </summary>
        /// <param name="party">OIDC down party.</param>
        /// <returns>OIDC down party.</returns>
        [ProducesResponseType(typeof(Api.OidcDownParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OidcDownParty>> PostOidcDownParty([FromBody] Api.OidcDownParty party) => await Post(party, ap => Task.FromResult(true), async (ap, mp) => await validateOAuthOidcLogic.ValidateResourceScopesAsync(ModelState, mp));

        /// <summary>
        /// Update OIDC down party.
        /// </summary>
        /// <param name="party">OIDC down party.</param>
        /// <returns>OIDC down party.</returns>
        [ProducesResponseType(typeof(Api.OidcDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OidcDownParty>> PutOidcDownParty([FromBody] Api.OidcDownParty party) => await Put(party, ap => Task.FromResult(true), async (ap, mp) => await validateOAuthOidcLogic.ValidateResourceScopesAsync(ModelState, mp));

        /// <summary>
        /// Delete OIDC down party.
        /// </summary>
        /// <param name="name">Party name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOidcDownParty(string name) => await Delete(name);
    }
}
