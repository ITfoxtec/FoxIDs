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
    [Obsolete($"Use {nameof(TResourceNamesController)} instead.")]
    public class TFilterResourceNameController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly EmbeddedResourceLogic embeddedResourceLogic;

        public TFilterResourceNameController(TelemetryScopedLogger logger, IMapper mapper, EmbeddedResourceLogic embeddedResourceLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.embeddedResourceLogic = embeddedResourceLogic;
        }

        /// <summary>
        /// Obsolete please use 'ResourceNames' instead.
        /// Filter resource name or ID.
        /// </summary>
        /// <param name="filterName">Filter resource name or ID.</param>
        /// <returns>Resource name.</returns>
        [ProducesResponseType(typeof(List<Api.ResourceName>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Obsolete($"Use {nameof(TResourceNamesController)} instead.")]
    public ActionResult<List<Api.ResourceName>> GetFilterResourceName(string filterName)
        {
            try
            {
                filterName = filterName?.Trim();
                var resourceEnvelope = embeddedResourceLogic.GetResourceEnvelope();
                var filderResourceNames = filterName.IsNullOrWhiteSpace() ? resourceEnvelope.Names : resourceEnvelope.Names.Where(r => r.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase) || (int.TryParse(filterName, out var filterId) && r.Id == filterId));
                return Ok(mapper.Map<List<Api.ResourceName>>(filderResourceNames.OrderBy(r => r.Id)));
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
