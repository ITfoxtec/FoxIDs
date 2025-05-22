using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using ITfoxtec.Identity;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Infrastructure.Filters;
using System;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class TUsageTenantsController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TUsageTenantsController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Get usage tenants.
        /// </summary>
        /// <param name="filterName">Filter usage tenant name.</param>
        /// <param name="paginationToken">The pagination token.</param>
        /// <returns>Tenants.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.Tenant>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.PaginationResponse<Api.Tenant>>> GetUsageTenants(string filterName, string paginationToken = null)
        {
            try
            {
                (var mTenants, var nextPaginationToken) = filterName.IsNullOrWhiteSpace() ?
                    await tenantDataRepository.GetManyAsync<Tenant>(whereQuery: t => t.ForUsage == true && t.Name != Constants.Routes.MasterTenantName, paginationToken: paginationToken) :
                    await tenantDataRepository.GetManyAsync<Tenant>(whereQuery: t => t.ForUsage == true && t.Name != Constants.Routes.MasterTenantName && t.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase), paginationToken: paginationToken);

                var response = new Api.PaginationResponse<Api.Tenant>
                {
                    Data = new HashSet<Api.Tenant>(mTenants.Count()),
                    PaginationToken = nextPaginationToken,
                };
                foreach (var mTenant in mTenants.OrderBy(t => t.Name))
                {
                    response.Data.Add(mapper.Map<Api.Tenant>(mTenant));
                }
                return Ok(response);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.Tenant).Name}' by filter name '{filterName}'.");
                    return NotFound(typeof(Api.Tenant).Name, filterName);
                }
                throw;
            }
        }
    }
}
