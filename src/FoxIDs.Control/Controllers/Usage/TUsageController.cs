using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Infrastructure.Filters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class TUsageController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public object MTenant { get; private set; }

        public TUsageController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Get usage.
        /// </summary>
        /// <param name="usageRequest">Usage request.</param>
        /// <returns>Used.</returns>
        [ProducesResponseType(typeof(Api.UpdateUsageRequest), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.UpdateUsageRequest>> GetUsage(Api.UsageRequest usageRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(usageRequest)) return BadRequest(ModelState);
                usageRequest.TenantName = usageRequest.TenantName?.ToLower();

                var mUsed = await tenantDataRepository.GetAsync<Used>(await Used.IdFormatAsync(usageRequest.TenantName, usageRequest.Year, usageRequest.Month));
                return Ok(mapper.Map<Api.UpdateUsageRequest>(mUsed));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.UpdateUsageRequest).Name}' by tenant name '{usageRequest.TenantName}', year '{usageRequest.Year}' and month '{usageRequest.Month}'.");
                    return NotFound(typeof(Api.UpdateUsageRequest).Name, $"{usageRequest.TenantName}/{usageRequest.Year}/{usageRequest.Month}");
                }
                throw;
            }
        }

        /// <summary>
        /// Create usage.
        /// </summary>
        /// <param name="usageRequest">Usage request.</param>
        /// <returns>Used.</returns>
        [ProducesResponseType(typeof(Api.Used), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.Used>> PostUsage([FromBody] Api.UpdateUsageRequest usageRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(usageRequest)) return BadRequest(ModelState);
                usageRequest.TenantName = usageRequest.TenantName.ToLower();

                var mUsed = mapper.Map<Used>(usageRequest);
                await tenantDataRepository.CreateAsync(mUsed);

                return Created(mapper.Map<Api.Used>(mUsed));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create '{typeof(Api.Used).Name}' by tenant name '{usageRequest.TenantName}', year '{usageRequest.Year}' and month '{usageRequest.Month}'.");
                    return Conflict(typeof(Api.Tenant).Name, $"{usageRequest.TenantName}/{usageRequest.Year}/{usageRequest.Month}");
                }
                throw;
            }
        }

        /// <summary>
        /// Update usage.
        /// </summary>
        /// <param name="usageRequest">Usage request.</param>
        /// <returns>Used.</returns>
        [ProducesResponseType(typeof(Api.Used), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Used>> PutUsage([FromBody] Api.UpdateUsageRequest usageRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(usageRequest)) return BadRequest(ModelState);
                usageRequest.TenantName = usageRequest.TenantName.ToLower();

                var mUsed = await tenantDataRepository.GetAsync<Used>(await Used.IdFormatAsync(usageRequest.TenantName, usageRequest.Year, usageRequest.Month));
                if (mUsed.Items?.Count() > 0) 
                {
                    mUsed.Items = mapper.Map<List<UsedItem>>(usageRequest.Items);
                }
                else
                {
                    mUsed.Items = null;
                }
                await tenantDataRepository.UpdateAsync(mUsed);

                return Ok(mapper.Map<Api.Used>(mUsed));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.Used).Name}' by tenant name '{usageRequest.TenantName}', year '{usageRequest.Year}' and month '{usageRequest.Month}'.");
                    return NotFound(typeof(Api.Tenant).Name, $"{usageRequest.TenantName}/{usageRequest.Year}/{usageRequest.Month}");
                }
                throw;
            }
        }
    }
}
