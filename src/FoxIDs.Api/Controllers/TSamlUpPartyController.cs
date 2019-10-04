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
    /// Saml up party api.
    /// </summary>
    public class TSamlUpPartyController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantService;
        private readonly ValidateSamlPartyLogic validateSamlPartyLogic;

        public TSamlUpPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantService, ValidateSamlPartyLogic validateSamlPartyLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantService = tenantService;
            this.validateSamlPartyLogic = validateSamlPartyLogic;
        }

        /// <summary>
        /// Get Saml up party.
        /// </summary>
        /// <param name="name">Party id.</param>
        /// <returns>Saml up party.</returns>
        [ProducesResponseType(typeof(Api.SamlUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SamlUpParty>> Get(string name)
        {
            try
            {
                if (!ModelState.TryValidateParameterAsync(name, nameof(name))) return BadRequest(ModelState);

                var samlUpParty = await tenantService.GetAsync<SamlUpParty>(await UpParty.IdFormat(RouteBinding, name));
                return Ok(mapper.Map<Api.SamlUpParty>(samlUpParty));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"Get '{nameof(Api.SamlUpParty)}' by name '{name}'.");
                    return NotFound(nameof(Api.SamlUpParty), name);
                }
                throw;
            }
        }

        /// <summary>
        /// Create Saml up party.
        /// </summary>
        /// <param name="response">Saml up party.</param>
        /// <returns>Saml up party.</returns>
        [ProducesResponseType(typeof(Api.SamlUpParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.SamlUpParty>> Post([FromBody] Api.SamlUpParty response)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(response)) return BadRequest(ModelState);
                if (!validateSamlPartyLogic.ValidateSignatureAlgorithm(ModelState, response)) return BadRequest(ModelState);                
               
                var samlUpParty = mapper.Map<SamlUpParty>(response);
                await tenantService.CreateAsync(samlUpParty);

                return Created(mapper.Map<Api.SamlUpParty>(samlUpParty));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Create '{nameof(Api.SamlUpParty)}' by name '{response.Name}'.");
                    return Conflict(nameof(Api.SamlUpParty), response.Name);
                }
                throw;
            }
        }

        /// <summary>
        /// Update Saml up party.
        /// </summary>
        /// <param name="response">Saml up party.</param>
        /// <returns>Saml up party.</returns>
        [ProducesResponseType(typeof(Api.SamlUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SamlUpParty>> Put([FromBody] Api.SamlUpParty response)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(response)) return BadRequest(ModelState);
                if (!validateSamlPartyLogic.ValidateSignatureAlgorithm(ModelState, response)) return BadRequest(ModelState);

                var samlUpParty = mapper.Map<SamlUpParty>(response);
                await tenantService.UpdateAsync(samlUpParty);

                return Ok(mapper.Map<Api.SamlUpParty>(samlUpParty));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"Update '{nameof(Api.SamlUpParty)}' by name '{response.Name}'.");
                    return NotFound(nameof(Api.SamlUpParty), response.Name);
                }
                throw;
            }
        }

        /// <summary>
        /// Delete Saml up party.
        /// </summary>
        /// <param name="name">Party id.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string name)
        {
            try
            {
                if (!ModelState.TryValidateParameterAsync(name, nameof(name))) return BadRequest(ModelState);

                await tenantService.DeleteAsync<SamlUpParty>(await UpParty.IdFormat(RouteBinding, name));
                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"Delete '{nameof(Api.SamlUpParty)}' by id '{name}'.");
                    return NotFound(nameof(Api.SamlUpParty), name);
                }
                throw;
            }
        }
    }
}
