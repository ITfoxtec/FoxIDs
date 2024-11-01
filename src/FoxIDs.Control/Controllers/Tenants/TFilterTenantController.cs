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
    public class TFilterTenantController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TFilterTenantController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Filter tenant.
        /// </summary>
        /// <param name="filterName">Filter tenant name.</param>
        /// <returns>Tenants.</returns>
        [ProducesResponseType(typeof(HashSet<Api.Tenant>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HashSet<Api.Tenant>>> GetFilterTenant(string filterName, string filterCustomDomain)
        {
            try
            {
                (var mTenants, _) = await GetFilterTenantInternalAsync(filterName, filterCustomDomain);
                var aTenants = new HashSet<Api.Tenant>(mTenants.Count());
                foreach (var mTenant in mTenants.OrderBy(t => t.Name))
                {
                    aTenants.Add(mapper.Map<Api.Tenant>(mTenant));
                }
                return Ok(aTenants);
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

        private ValueTask<(IReadOnlyCollection<Tenant> items, string paginationToken)> GetFilterTenantInternalAsync(string filterName, string filterCustomDomain)
        {
            if (filterName.IsNullOrWhiteSpace() && filterCustomDomain.IsNullOrWhiteSpace())
            {
                return tenantDataRepository.GetListAsync<Tenant>(whereQuery: t => t.Name != Constants.Routes.MasterTenantName);
            }
            else if(!filterName.IsNullOrWhiteSpace() && filterCustomDomain.IsNullOrWhiteSpace())
            {
                return tenantDataRepository.GetListAsync<Tenant>(whereQuery: t => t.Name != Constants.Routes.MasterTenantName && t.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase));
            }
            else if (filterName.IsNullOrWhiteSpace() && !filterCustomDomain.IsNullOrWhiteSpace())
            {
                return tenantDataRepository.GetListAsync<Tenant>(whereQuery: t => t.Name != Constants.Routes.MasterTenantName && t.CustomDomain.Contains(filterCustomDomain, StringComparison.CurrentCultureIgnoreCase));
            }
            else
            {
                return tenantDataRepository.GetListAsync<Tenant>(whereQuery: t => t.Name != Constants.Routes.MasterTenantName && t.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase) || t.CustomDomain.Contains(filterCustomDomain, StringComparison.CurrentCultureIgnoreCase));
            }
        }
    }
}
