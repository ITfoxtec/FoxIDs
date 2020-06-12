using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AutoMapper;

namespace FoxIDs.Controllers
{
    public class MResourceController : MasterApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly IMasterRepository masterService;

        public MResourceController(TelemetryScopedLogger logger, IMapper mapper, IMasterRepository masterService) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.masterService = masterService;
        }

        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> PostResource([FromBody] Api.Resource model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var resourceEnvelope = mapper.Map<ResourceEnvelope>(model);
            resourceEnvelope.Id = ResourceEnvelope.IdFormat(new MasterDocument.IdKey());
    
            await masterService.SaveAsync(resourceEnvelope);

            return NoContent();
        }
    }
}
