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
    /// Saml down party api.
    /// </summary>
    public class TSamlDownPartyController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantService;
        private readonly ValidateSamlPartyLogic validateSamlPartyLogic;
        private readonly ValidatePartyLogic validatePartyLogic;

        public TSamlDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantService, ValidateSamlPartyLogic validateSamlPartyLogic, ValidatePartyLogic validatePartyLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantService = tenantService;
            this.validateSamlPartyLogic = validateSamlPartyLogic;
            this.validatePartyLogic = validatePartyLogic;
        }

        /// <summary>
        /// Get Saml down party.
        /// </summary>
        /// <param name="name">Party id.</param>
        /// <returns>Saml down party.</returns>
        [ProducesResponseType(typeof(Api.SamlDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SamlDownParty>> Get(string name)
        {
            try
            {
                if (!ModelState.TryValidateParameterAsync(name, nameof(name))) return BadRequest(ModelState);

                var samlDownParty = await tenantService.GetAsync<SamlDownParty>(await DownParty.IdFormat(RouteBinding, name));
                return Ok(mapper.Map<Api.SamlDownParty>(samlDownParty));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"Get '{nameof(Api.SamlDownParty)}' by name '{name}'.");
                    return NotFound(nameof(Api.SamlDownParty), name);
                }
                throw;
            }
        }

        /// <summary>
        /// Create Saml down party.
        /// </summary>
        /// <param name="response">Saml down party.</param>
        /// <returns>Saml down party.</returns>
        [ProducesResponseType(typeof(Api.SamlDownParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.SamlDownParty>> Post([FromBody] Api.SamlDownParty response)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(response)) return BadRequest(ModelState);
                if (!validateSamlPartyLogic.ValidateSignatureAlgorithm(ModelState, response)) return BadRequest(ModelState);                
               
                var samlDownParty = mapper.Map<SamlDownParty>(response);
                if (!await validatePartyLogic.ValidateAllowUpParties(ModelState, nameof(response.AllowUpPartyNames), samlDownParty)) return BadRequest(ModelState);
                await tenantService.CreateAsync(samlDownParty);

                return Created(mapper.Map<Api.SamlDownParty>(samlDownParty));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Create '{nameof(Api.SamlDownParty)}' by name '{response.Name}'.");
                    return Conflict(nameof(Api.SamlDownParty), response.Name);
                }
                throw;
            }
        }

        /// <summary>
        /// Downdate Saml down party.
        /// </summary>
        /// <param name="response">Saml down party.</param>
        /// <returns>Saml down party.</returns>
        [ProducesResponseType(typeof(Api.SamlDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SamlDownParty>> Put([FromBody] Api.SamlDownParty response)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(response)) return BadRequest(ModelState);
                if (!validateSamlPartyLogic.ValidateSignatureAlgorithm(ModelState, response)) return BadRequest(ModelState);

                var samlDownParty = mapper.Map<SamlDownParty>(response);
                if (!await validatePartyLogic.ValidateAllowUpParties(ModelState, nameof(response.AllowUpPartyNames), samlDownParty)) return BadRequest(ModelState);
                await tenantService.UpdateAsync(samlDownParty);

                return Ok(mapper.Map<Api.SamlDownParty>(samlDownParty));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"Downdate '{nameof(Api.SamlDownParty)}' by name '{response.Name}'.");
                    return NotFound(nameof(Api.SamlDownParty), response.Name);
                }
                throw;
            }
        }

        /// <summary>
        /// Delete Saml down party.
        /// </summary>
        /// <param name="name">Party id.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string name)
        {
            try
            {
                if (!ModelState.TryValidateParameterAsync(name, nameof(name))) return BadRequest(ModelState);

                await tenantService.DeleteAsync<SamlDownParty>(await DownParty.IdFormat(RouteBinding, name));
                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"Delete '{nameof(Api.SamlDownParty)}' by id '{name}'.");
                    return NotFound(nameof(Api.SamlDownParty), name);
                }
                throw;
            }
        }
    }
}
