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
    /// Login up party api.
    /// </summary>
    public class TLoginUpPartyController : GenericPartyApiController<Api.LoginUpParty, LoginUpParty>
    {
        public TLoginUpPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, ValidatePartyLogic validatePartyLogic) : base(logger, mapper, tenantRepository, validatePartyLogic)
        { }

        /// <summary>
        /// Get Login up party.
        /// </summary>
        /// <param name="name">Party name.</param>
        /// <returns>Login up party.</returns>
        [ProducesResponseType(typeof(Api.LoginUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LoginUpParty>> GetLoginUpParty(string name) => await Get(name);

        /// <summary>
        /// Create Login up party.
        /// </summary>
        /// <param name="party">Login up party.</param>
        /// <returns>Login up party.</returns>
        [ProducesResponseType(typeof(Api.LoginUpParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.LoginUpParty>> PostLoginUpParty([FromBody] Api.LoginUpParty party) => await Post(party, ap => Task.FromResult(true), (ap, mp) => Task.FromResult(true));

        /// <summary>
        /// Update Login up party.
        /// </summary>
        /// <param name="party">Login up party.</param>
        /// <returns>Login up party.</returns>
        [ProducesResponseType(typeof(Api.LoginUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LoginUpParty>> PutLoginUpParty([FromBody] Api.LoginUpParty party) => await Put(party, ap => Task.FromResult(true), (ap, mp) => Task.FromResult(true));

        /// <summary>
        /// Delete Login up party.
        /// </summary>
        /// <param name="name">Party name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteLoginUpParty(string name) => await Delete(name);
    }
}
