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
    public class TTenantsController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TTenantsController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Get tenants.
        /// </summary>
        /// <param name="filterName">Filter by tenant name.</param>
        /// <param name="filterCustomDomain">Filter by custom domain.</param>
        /// <param name="paginationToken">The pagination token.</param>
        /// <returns>Tenants.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.Tenant>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Api.PaginationResponse<Api.Tenant>>> GetTenants(string filterName, string filterCustomDomain, string paginationToken = null)
        {
            try
            {
                filterName = filterName?.Trim();
                filterCustomDomain = filterCustomDomain?.Trim();
                (var mTenants, var nextPaginationToken) = await GetFilterTenantInternalAsync(filterName, filterCustomDomain, paginationToken);

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

        private ValueTask<(IReadOnlyCollection<Tenant> items, string nextPaginationToken)> GetFilterTenantInternalAsync(string filterName, string filterCustomDomain, string paginationToken)
        {
            if (filterName.IsNullOrWhiteSpace() && filterCustomDomain.IsNullOrWhiteSpace())
            {
                return tenantDataRepository.GetManyAsync<Tenant>(whereQuery: t => (t.ForUsage != true || !t.ForUsage.HasValue) && t.Name != Constants.Routes.MasterTenantName, paginationToken: paginationToken);
            }
            else if(!filterName.IsNullOrWhiteSpace() && filterCustomDomain.IsNullOrWhiteSpace())
            {
                return tenantDataRepository.GetManyAsync<Tenant>(whereQuery: t => (t.ForUsage != true || !t.ForUsage.HasValue) && t.Name != Constants.Routes.MasterTenantName && t.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase), paginationToken: paginationToken);
            }
            else if (filterName.IsNullOrWhiteSpace() && !filterCustomDomain.IsNullOrWhiteSpace())
            {
                return tenantDataRepository.GetManyAsync<Tenant>(whereQuery: t => (t.ForUsage != true || !t.ForUsage.HasValue) && t.Name != Constants.Routes.MasterTenantName && t.CustomDomain.Contains(filterCustomDomain, StringComparison.CurrentCultureIgnoreCase), paginationToken: paginationToken);
            }
            else
            {
                return tenantDataRepository.GetManyAsync<Tenant>(whereQuery: t => (t.ForUsage != true || !t.ForUsage.HasValue) && t.Name != Constants.Routes.MasterTenantName && t.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase) || t.CustomDomain.Contains(filterCustomDomain, StringComparison.CurrentCultureIgnoreCase), paginationToken: paginationToken);
            }
        }
    }
}
