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
    [Obsolete($"Use {nameof(TUsageTenantsController)} instead.")]
    public class TFilterUsageTenantController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TFilterUsageTenantController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Obsolete please use 'UsageTenants' instead.
        /// Filter usage tenant.
        /// </summary>
        /// <param name="filterName">Filter usage tenant name.</param>
        /// <returns>Tenants.</returns>
        [ProducesResponseType(typeof(HashSet<Api.Tenant>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Obsolete($"Use {nameof(TUsageTenantsController)} instead.")]
    public async Task<ActionResult<HashSet<Api.Tenant>>> GetFilterUsageTenant(string filterName)
        {
            try
            {
                filterName = filterName?.Trim();
                (var mTenants, _) = filterName.IsNullOrWhiteSpace() ?
                    await tenantDataRepository.GetManyAsync<Tenant>(whereQuery: t => t.ForUsage == true && t.Name != Constants.Routes.MasterTenantName) :
                    await tenantDataRepository.GetManyAsync<Tenant>(whereQuery: t => t.ForUsage == true && t.Name != Constants.Routes.MasterTenantName && t.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase));

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
    }
}
