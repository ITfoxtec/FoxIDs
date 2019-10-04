using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;

namespace FoxIDs.Controllers
{
    /// <summary>
    /// Login up party api.
    /// </summary>
    public class TLoginUpPartyController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantService;

        public TLoginUpPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantService) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantService = tenantService;
        }

        /// <summary>
        /// Get Login up party.
        /// </summary>
        /// <param name="name">Party id.</param>
        /// <returns>Login up party.</returns>
        [ProducesResponseType(typeof(Api.LoginUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LoginUpParty>> Get(string name)
        {
            try
            {
                if (!ModelState.TryValidateParameterAsync(name, nameof(name))) return BadRequest(ModelState);

                var loginUpParty = await tenantService.GetAsync<LoginUpParty>(await UpParty.IdFormat(RouteBinding, name));
                return Ok(mapper.Map<Api.LoginUpParty>(loginUpParty));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"Get '{nameof(Api.LoginUpParty)}' by name '{name}'.");
                    return NotFound(nameof(Api.LoginUpParty), name);
                }
                throw;
            }
        }

        /// <summary>
        /// Create Login up party.
        /// </summary>
        /// <param name="response">Login up party.</param>
        /// <returns>Login up party.</returns>
        [ProducesResponseType(typeof(Api.LoginUpParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.LoginUpParty>> Post([FromBody] Api.LoginUpParty response)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(response)) return BadRequest(ModelState);

                var loginUpParty = mapper.Map<LoginUpParty>(response);
                await tenantService.CreateAsync(loginUpParty);

                return Created(mapper.Map<Api.LoginUpParty>(loginUpParty));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Create '{nameof(Api.LoginUpParty)}' by name '{response.Name}'.");
                    return Conflict(nameof(Api.LoginUpParty), response.Name);
                }
                throw;
            }
        }

        /// <summary>
        /// Update Login up party.
        /// </summary>
        /// <param name="response">Login up party.</param>
        /// <returns>Login up party.</returns>
        [ProducesResponseType(typeof(Api.LoginUpParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LoginUpParty>> Put([FromBody] Api.LoginUpParty response)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(response)) return BadRequest(ModelState);

                var loginUpParty = mapper.Map<LoginUpParty>(response);
                await tenantService.UpdateAsync(loginUpParty);

                return Ok(mapper.Map<Api.LoginUpParty>(loginUpParty));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"Update '{nameof(Api.LoginUpParty)}' by name '{response.Name}'.");
                    return NotFound(nameof(Api.LoginUpParty), response.Name);
                }
                throw;
            }
        }

        /// <summary>
        /// Delete Login up party.
        /// </summary>
        /// <param name="name">Party id.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string name)
        {
            try
            {
                if (!ModelState.TryValidateParameterAsync(name, nameof(name))) return BadRequest(ModelState);

                await tenantService.DeleteAsync<LoginUpParty>(await UpParty.IdFormat(RouteBinding, name));
                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"Delete '{nameof(Api.LoginUpParty)}' by id '{name}'.");
                    return NotFound(nameof(Api.LoginUpParty), name);
                }
                throw;
            }
        }
    }
}
