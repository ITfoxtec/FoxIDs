using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;
using FoxIDs.Logic;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Infrastructure.Filters;
using System;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class TTenantController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;
        private readonly MasterTenantLogic masterTenantLogic;

        public TTenantController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, MasterTenantLogic masterTenantLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
            this.masterTenantLogic = masterTenantLogic;
        }

        /// <summary>
        /// Get tenant.
        /// </summary>
        /// <param name="name">Tenant name.</param>
        /// <returns>Tenant.</returns>
        [ProducesResponseType(typeof(Api.Tenant), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Tenant>> GetTenant(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);
                name = name?.ToLower();

                var MTenant = await tenantRepository.GetTenantByNameAsync(name);
                return Ok(mapper.Map<Api.Tenant>(MTenant));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.Tenant).Name}' by name '{name}'.");
                    return NotFound(typeof(Api.Tenant).Name, name);
                }
                throw;
            }
        }

        /// <summary>
        /// Create tenant.
        /// </summary>
        /// <param name="tenant">Tenant.</param>
        /// <returns>Tenant.</returns>
        [ProducesResponseType(typeof(Api.Tenant), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<Api.Tenant>> PostTenant([FromBody] Api.CreateTenantRequest tenant)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(tenant)) return BadRequest(ModelState);
                tenant.Name = tenant.Name?.ToLower();
                tenant.AdministratorEmail = tenant.AdministratorEmail?.ToLower();

                var mTenant = mapper.Map<Tenant>(tenant);
                await tenantRepository.CreateAsync(mTenant);

                await masterTenantLogic.CreateMasterTrackDocumentAsync(tenant.Name);
                var mLoginUpParty = await masterTenantLogic.CreateLoginDocumentAsync(tenant.Name);
                await masterTenantLogic.CreateFirstAdminUserDocumentAsync(tenant.Name, tenant.AdministratorEmail, tenant.AdministratorPassword);
                await masterTenantLogic.CreateFoxIDsControlApiResourceDocumentAsync(tenant.Name);
                await masterTenantLogic.CreateControlClientDocmentAsync(tenant.Name, tenant.ControlClientBaseUri, mLoginUpParty);

                return Created(mapper.Map<Api.Tenant>(mTenant));
            }
            catch (AccountException aex)
            {
                ModelState.TryAddModelError(nameof(tenant.AdministratorPassword), aex.Message);
                return BadRequest(ModelState, aex);
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create '{typeof(Api.Tenant).Name}' by name '{tenant.Name}'.");
                    return Conflict(typeof(Api.Tenant).Name, tenant.Name);
                }
                throw;
            }
        }

        /// <summary>
        /// Delete tenant.
        /// </summary>
        /// <param name="name">Tenant name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTenant(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);
                name = name?.ToLower();

                if (name.Equals(Constants.Routes.MasterTenantName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("The master track can not be deleted.");
                }

                //TODO delete all sub elements
                // Waiting for https://feedback.azure.com/forums/263030-azure-cosmos-db/suggestions/17296813-add-the-ability-to-delete-all-data-in-a-partition
                //            Add the ability to delete ALL data in a partition
                var mTracks = await tenantRepository.GetListAsync<Track>(new Track.IdKey { TenantName = name }, whereQuery: p => p.DataType.Equals("track"));
                foreach(var mTrack in mTracks)
                {
                    await tenantRepository.DeleteListAsync<DefaultElement>(new Track.IdKey { TenantName = name, TrackName = mTrack.Name });
                    await tenantRepository.DeleteAsync<Track>(mTrack.Id);
                }
                await tenantRepository.DeleteAsync<Tenant>(await Tenant.IdFormat(name));

                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.Tenant).Name}' by name '{name}'.");
                    return NotFound(typeof(Api.Tenant).Name, name);
                }
                throw;
            }
        }
    }
}
