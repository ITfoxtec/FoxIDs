using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using FoxIDs.Logic;

namespace FoxIDs.Controllers
{
    /// <summary>
    /// OpenID Connect down party api.
    /// </summary>
    public class TOidcDownPartyController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantService;
        private readonly ValidatePartyLogic validatePartyLogic;

        public TOidcDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantService, ValidatePartyLogic validatePartyLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantService = tenantService;
            this.validatePartyLogic = validatePartyLogic;
        }

        /// <summary>
        /// Get Oidc down party.
        /// </summary>
        /// <param name="name">Party id.</param>
        /// <returns>Oidc down party.</returns>
        [ProducesResponseType(typeof(Api.OidcDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OidcDownParty>> Get(string name)
        {
            try
            {
                if (!ModelState.TryValidateParameterAsync(name, nameof(name))) return BadRequest(ModelState);

                var oidcDownParty = await tenantService.GetAsync<OidcDownParty>(await DownParty.IdFormat(RouteBinding, name));
                return Ok(mapper.Map<Api.OidcDownParty>(oidcDownParty));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"Get '{nameof(Api.OidcDownParty)}' by name '{name}'.");
                    return NotFound(nameof(Api.OidcDownParty), name);
                }
                throw;
            }
        }

        /// <summary>
        /// Create Oidc down party.
        /// </summary>
        /// <param name="response">Oidc down party.</param>
        /// <returns>Oidc down party.</returns>
        [ProducesResponseType(typeof(Api.OidcDownParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OidcDownParty>> Post([FromBody] Api.OidcDownParty response)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(response)) return BadRequest(ModelState);

                var oidcDownParty = mapper.Map<OidcDownParty>(response);
                if (!await validatePartyLogic.ValidateAllowUpParties(ModelState, nameof(response.AllowUpPartyNames), oidcDownParty)) return BadRequest(ModelState);
                if (!await validatePartyLogic.ValidateResourceScopes(ModelState, oidcDownParty)) return BadRequest(ModelState);
                await tenantService.CreateAsync(oidcDownParty);

                return Created(mapper.Map<Api.OidcDownParty>(oidcDownParty));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Create '{nameof(Api.OidcDownParty)}' by name '{response.Name}'.");
                    return Conflict(nameof(Api.OidcDownParty), response.Name);
                }
                throw;
            }
        }

        /// <summary>
        /// Update Oidc down party.
        /// </summary>
        /// <param name="response">Oidc down party.</param>
        /// <returns>Oidc down party.</returns>
        [ProducesResponseType(typeof(Api.OidcDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.OidcDownParty>> Put([FromBody] Api.OidcDownParty response)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(response)) return BadRequest(ModelState);

                var oidcDownParty = mapper.Map<OidcDownParty>(response);
                if (!await validatePartyLogic.ValidateAllowUpParties(ModelState, nameof(response.AllowUpPartyNames), oidcDownParty)) return BadRequest(ModelState);
                if (!await validatePartyLogic.ValidateResourceScopes(ModelState, oidcDownParty)) return BadRequest(ModelState);
                await tenantService.UpdateAsync(oidcDownParty);

                return Ok(mapper.Map<Api.OidcDownParty>(oidcDownParty));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"Update '{nameof(Api.OidcDownParty)}' by name '{response.Name}'.");
                    return NotFound(nameof(Api.OidcDownParty), response.Name);
                }
                throw;
            }
        }

        /// <summary>
        /// Delete Oidc down party.
        /// </summary>
        /// <param name="name">Party id.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string name)
        {
            try
            {
                if (!ModelState.TryValidateParameterAsync(name, nameof(name))) return BadRequest(ModelState);

                await tenantService.DeleteAsync<OidcDownParty>(await DownParty.IdFormat(RouteBinding, name));
                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"Delete '{nameof(Api.OidcDownParty)}' by id '{name}'.");
                    return NotFound(nameof(Api.OidcDownParty), name);
                }
                throw;
            }
        }
    }
}
