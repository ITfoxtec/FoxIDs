﻿using AutoMapper;
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
        private readonly ITenantRepository tenantRepository;

        public TFilterTenantController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
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
                var mTenants = filterName.IsNullOrWhiteSpace() ? await tenantRepository.GetListAsync<Tenant>() : await tenantRepository.GetListAsync<Tenant>(whereQuery: t => t.Name.Contains(filterName));
                var aTenants = new HashSet<Api.Tenant>(mTenants.Count());
                foreach(var mTenant in mTenants.OrderBy(t => t.Name))
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
