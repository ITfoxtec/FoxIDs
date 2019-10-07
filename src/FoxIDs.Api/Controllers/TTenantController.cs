using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    public class TTenantController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantService;

        public TTenantController(TelemetryScopedLogger logger, ITenantRepository tenantService) : base(logger)
        {
            this.logger = logger;
            this.tenantService = tenantService;
        }

        public async Task<IActionResult> GetTenant()
        {
            //var documentQuery = await tenantService.GetQuery("testcorp").AsDocumentQuery().ExecuteNextAsync<Tenant>();
            //var tenant = documentQuery.FirstOrDefault();
        /*    if (tenant == null) */return NotFound();
            //return Json(tenant);
        }

        public async Task<IActionResult> PostTenant([FromBody]Tenant tenant)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            await tenantService.SaveAsync(tenant);
            return Ok();
        }

    }
}
