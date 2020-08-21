using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;

namespace FoxIDs.Controllers
{
    public class TResourceNamesController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly IMasterRepository masterRepository;

        public TResourceNamesController(TelemetryScopedLogger logger, IMapper mapper, IMasterRepository masterRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.masterRepository = masterRepository;
        }

        /// <summary>
        /// Get resource names.
        /// </summary>
        /// <returns>Resource names.</returns>
        [ProducesResponseType(typeof(List<Api.ResourceName>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<Api.ResourceName>>> GetResourceNames()
        {
            try
            {
                var resourceEnvelope = await masterRepository.GetAsync<ResourceEnvelope>(ResourceEnvelope.IdFormat(new MasterDocument.IdKey()));
                return Ok(mapper.Map<List<Api.ResourceName>>(resourceEnvelope.Names));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(ResourceEnvelope).Name}'.");
                    return NotFound(typeof(ResourceEnvelope).Name, "master");
                }
                throw;
            }
        }
    }
}
