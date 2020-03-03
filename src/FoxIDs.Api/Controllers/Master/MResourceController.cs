using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    public class MResourceController : MasterApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMasterRepository masterService;

        public MResourceController(TelemetryScopedLogger logger, IMasterRepository masterService) : base(logger)
        {
            this.logger = logger;
            this.masterService = masterService;
        }

        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> PostResource([FromBody] Api.Resource model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var resourceEnvelope = new ResourceEnvelope
            {
                Id = ResourceEnvelope.IdFormat(new MasterDocument.IdKey()),
                SupportedCultures = model.SupportedCultures,
                Names = model.Names,
                Resources = model.Resources,
            };

            await masterService.SaveAsync(resourceEnvelope);

            return NoContent();
        }
    }
}
