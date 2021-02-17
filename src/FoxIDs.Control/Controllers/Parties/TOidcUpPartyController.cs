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
    /// OIDC up-party API.
    /// </summary>
    public class TOidcUpPartyController : GenericPartyApiController<Api.OidcUpParty, OidcUpParty>
    {
        private readonly ValidateOAuthOidcPartyLogic validateOAuthOidcPartyLogic;

        public TOidcUpPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, ValidateGenericPartyLogic validateGenericPartyLogic, ValidateOAuthOidcPartyLogic validateOAuthOidcPartyLogic) : base(logger, mapper, tenantRepository, validateGenericPartyLogic)
        {
            this.validateOAuthOidcPartyLogic = validateOAuthOidcPartyLogic;
        }

        /// <summary>
        /// Get OIDC up-party.
        /// </summary>
        /// <param name="name">Party name.</param>
        /// <returns>OIDC up-party.</returns>
        [ProducesResponseType(typeof(Api.OidcUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OidcUpParty>> GetOidcUpParty(string name) => await Get(name);

        /// <summary>
        /// Create OIDC up-party.
        /// </summary>
        /// <param name="party">OIDC up-party.</param>
        /// <returns>OIDC up-party.</returns>
        [ProducesResponseType(typeof(Api.OidcUpParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OidcUpParty>> PostOidcUpParty([FromBody] Api.OidcUpParty party) => await Post(party, ap => new ValueTask<bool>(true), async (ap, mp) => await Task.FromResult(validateOAuthOidcPartyLogic.ValidateModel(ModelState, mp)));

        /// <summary>
        /// Update OIDC up-party.
        /// </summary>
        /// <param name="party">OIDC up-party.</param>
        /// <returns>OIDC up-party.</returns>
        [ProducesResponseType(typeof(Api.OidcUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OidcUpParty>> PutOidcUpParty([FromBody] Api.OidcUpParty party) => await Put(party, ap => new ValueTask<bool>(true), async (ap, mp) => await Task.FromResult(validateOAuthOidcPartyLogic.ValidateModel(ModelState, mp)));

        /// <summary>
        /// Delete OIDC up-party.
        /// </summary>
        /// <param name="name">Party name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOidcUpParty(string name) => await Delete(name);
    }
}
