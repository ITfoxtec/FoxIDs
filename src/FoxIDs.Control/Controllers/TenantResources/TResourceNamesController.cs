using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using ITfoxtec.Identity;
using FoxIDs.Logic;
using System;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TResourceNamesController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly EmbeddedResourceLogic embeddedResourceLogic;

        public TResourceNamesController(TelemetryScopedLogger logger, IMapper mapper, EmbeddedResourceLogic embeddedResourceLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.embeddedResourceLogic = embeddedResourceLogic;
        }

        /// <summary>
        /// Get resource names and IDs.
        /// </summary>
        /// <param name="filterName">Filter resource name or ID.</param>
        /// <returns>Resource names.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.ResourceName>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Api.PaginationResponse<Api.ResourceName>> GetResourceNames(string filterName, string paginationToken = null)
        {
            try
            {
                var resourceEnvelope = embeddedResourceLogic.GetResourceEnvelope();
                var filderResourceNames = filterName.IsNullOrWhiteSpace() ? resourceEnvelope.Names : resourceEnvelope.Names.Where(r => r.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase) || (int.TryParse(filterName.Trim(), out var filterId) && r.Id == filterId));

                var response = new Api.PaginationResponse<Api.ResourceName>
                {
                    Data = mapper.Map<HashSet<Api.ResourceName>>(filderResourceNames.OrderBy(r => r.Id)),
                };

                return Ok(response);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(ResourceEnvelope).Name}'.");
                    return NotFound(typeof(ResourceEnvelope).Name, "master");
                }
                throw;
            }
        }
    }
}
