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
using System.Linq;
using ITfoxtec.Identity;

namespace FoxIDs.Controllers
{
    public class TFilterResourceNameController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly IMasterRepository masterRepository;

        public TFilterResourceNameController(TelemetryScopedLogger logger, IMapper mapper, IMasterRepository masterRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.masterRepository = masterRepository;
        }

        /// <summary>
        /// Filter resource name.
        /// </summary>
        /// <param name="filterName">Filter resource name.</param>
        /// <returns>Resource name.</returns>
        [ProducesResponseType(typeof(List<Api.ResourceName>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<Api.ResourceName>>> GetFilterResourceName(string filterName)
        {
            try
            {
                var resourceEnvelope = await masterRepository.GetAsync<ResourceEnvelope>(ResourceEnvelope.IdFormat(new MasterDocument.IdKey()));
                var filderResourceNames = filterName.IsNullOrWhiteSpace() ? resourceEnvelope.Names : resourceEnvelope.Names.Where(r => r.Name.Contains(filterName, System.StringComparison.OrdinalIgnoreCase));
                return Ok(mapper.Map<List<Api.ResourceName>>(filderResourceNames.OrderBy(r => r.Name)));
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
