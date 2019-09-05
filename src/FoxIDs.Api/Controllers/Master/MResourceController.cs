using FoxIDs.Infrastructure;
using FoxIDs.Model;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    public class MResourceController : MasterApiController
    {
        private readonly TelemetryLogger logger;
        private readonly IMasterRepository masterService;

        public MResourceController(TelemetryLogger logger, IMasterRepository masterService, IApiDescriptionGroupCollectionProvider apiExplorer) : base(logger)
        {
            this.logger = logger;
            this.masterService = masterService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ResourceApiModel model)
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
