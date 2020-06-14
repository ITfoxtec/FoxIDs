using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using ITfoxtec.Identity;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Infrastructure.Filters;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class TFilterTenantController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantService;

        public TFilterTenantController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantService) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantService = tenantService;
        }

        /// <summary>
        /// Filter tenant.
        /// </summary>
        /// <param name="filterName">Filter tenant name.</param>
        /// <returns>Tenant.</returns>
        [ProducesResponseType(typeof(HashSet<Api.Tenant>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HashSet<Api.Tenant>>> GetFilterTenant(string filterName)
        {
            try
            {
                var mTenants = filterName.IsNullOrWhiteSpace() ? await tenantService.GetListAsync<Tenant>() : await tenantService.GetListAsync<Tenant>(whereQuery: t => t.Name.Contains(filterName));
                var aTenants = new HashSet<Api.Tenant>(mTenants.Count());
                throw new System.Exception();
                foreach(var mTenant in mTenants)
                {
                    aTenants.Add(mapper.Map<Api.Tenant>(mTenant));
                }
                return Ok(aTenants);
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.Tenant).Name}' by filter name '{filterName}'.");
                    return NotFound(typeof(Api.Tenant).Name, filterName);
                }
                throw;
            }
        }
    }
}
