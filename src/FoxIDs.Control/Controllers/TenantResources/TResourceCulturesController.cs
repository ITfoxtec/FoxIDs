using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using FoxIDs.Logic;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TResourceCulturesController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly EmbeddedResourceLogic embeddedResourceLogic;

        public TResourceCulturesController(TelemetryScopedLogger logger, EmbeddedResourceLogic embeddedResourceLogic) : base(logger)
        {
            this.logger = logger;
            this.embeddedResourceLogic = embeddedResourceLogic;
        }

        /// <summary>
        /// Get resource cultures.
        /// </summary>
        /// <returns>Resource cultures.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.ResourceName>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Api.PaginationResponse<Api.ResourceCulture>> GetResourceCultures(string paginationToken = null)
        {
            try
            {
                var resourceEnvelope = embeddedResourceLogic.GetResourceEnvelope();

                var response = new Api.PaginationResponse<Api.ResourceCulture>
                {
                    Data = resourceEnvelope.SupportedCultures.OrderBy(c => c).Select(c => new Api.ResourceCulture { Culture = c }).ToHashSet(),
                };

                return Ok(response);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get cultures '{typeof(ResourceEnvelope).Name}'.");
                    return NotFound($"{typeof(ResourceEnvelope).Name}.{nameof(ResourceEnvelope.SupportedCultures)}", "master");
                }
                throw;
            }
        }
    }
}
