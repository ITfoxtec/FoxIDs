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
using ITfoxtec.Identity;

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
        [ProducesResponseType(typeof(Api.Used), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Used>> GetUsage(Api.UsageRequest usageRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(usageRequest)) return BadRequest(ModelState);
                usageRequest.TenantName = usageRequest.TenantName?.ToLower();

                var mTenant = await tenantDataRepository.GetAsync<Tenant>(await Tenant.IdFormatAsync(usageRequest.TenantName));

                var mUsed = await tenantDataRepository.GetAsync<Used>(await Used.IdFormatAsync(usageRequest.TenantName, usageRequest.PeriodBeginDate.Year, usageRequest.PeriodBeginDate.Month));

                var aUsed = mapper.Map<Api.Used>(mUsed);
                aUsed.Currency = GetCulture(mTenant);
                return Ok(aUsed);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.Used).Name}' by tenant name '{usageRequest.TenantName}', year '{usageRequest.PeriodBeginDate.Year}' and month '{usageRequest.PeriodBeginDate.Month}'.");
                    return NotFound(typeof(Api.Used).Name, $"{usageRequest.TenantName}/{usageRequest.PeriodBeginDate.Year}/{usageRequest.PeriodBeginDate.Month}");
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

                var mTenant = await tenantDataRepository.GetAsync<Tenant>(await Tenant.IdFormatAsync(usageRequest.TenantName));

                var mUsed = mapper.Map<Used>(usageRequest);
                if(!usageRequest.PeriodEndDate.HasValue)
                {
                    mUsed.PeriodEndDate = mUsed.PeriodBeginDate.AddMonths(1).AddDays(-1);
                }
                mUsed.Items = mUsed.Items?.OrderBy(i => i.Type).ThenBy(i => i.Day).ToList();
                await tenantDataRepository.CreateAsync(mUsed);

                var aUsed = mapper.Map<Api.Used>(mUsed);
                aUsed.Currency = GetCulture(mTenant);
                return Created(aUsed);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create '{typeof(Api.Used).Name}' by tenant name '{usageRequest.TenantName}', year '{usageRequest.PeriodBeginDate.Year}' and month '{usageRequest.PeriodBeginDate.Month}'.");
                    return Conflict(typeof(Api.Used).Name, $"{usageRequest.TenantName}/{usageRequest.PeriodBeginDate.Year}/{usageRequest.PeriodBeginDate.Month}");
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

                var mTenant = await tenantDataRepository.GetAsync<Tenant>(await Tenant.IdFormatAsync(usageRequest.TenantName));

                var mUsed = await tenantDataRepository.GetAsync<Used>(await Used.IdFormatAsync(usageRequest.TenantName, usageRequest.PeriodBeginDate.Year, usageRequest.PeriodBeginDate.Month));
                if (usageRequest.Items?.Count() > 0) 
                {
                    mUsed.Items = mapper.Map<List<UsedItem>>(usageRequest.Items).OrderBy(i => i.Type).ThenBy(i => i.Day).ToList();
                }
                else
                {
                    mUsed.Items = null;
                }
                await tenantDataRepository.UpdateAsync(mUsed);

                var aUsed = mapper.Map<Api.Used>(mUsed);
                aUsed.Currency = GetCulture(mTenant);
                return Ok(aUsed);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.Used).Name}' by tenant name '{usageRequest.TenantName}', year '{usageRequest.PeriodBeginDate.Year}' and month '{usageRequest.PeriodBeginDate.Month}'.");
                    return NotFound(typeof(Api.Used).Name, $"{usageRequest.TenantName}/{usageRequest.PeriodBeginDate.Year}/{usageRequest.PeriodBeginDate.Month}");
                }
                throw;
            }
        }

        private string GetCulture(Tenant mTenant)
        {
            return mTenant.Currency.IsNullOrEmpty() ? Constants.Models.Currency.Eur : mTenant.Currency;
        }

        /// <summary>
        /// Delete usage.
        /// </summary>
        /// <param name="name">Usage name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUsage(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);
                name = name?.ToLower();

                var nameSplit = name.Split('/');
                var year = 0;
                var month = 0;
                if (nameSplit.Length != 3 || !int.TryParse(nameSplit[1], out year) || !int.TryParse(nameSplit[2], out month))
                {
                    throw new ArgumentException($"Invalid name '{name}' format.", nameof(name));
                }
                var mUsed = await tenantDataRepository.GetAsync<Used>(await Used.IdFormatAsync(nameSplit[0], year, month));
                if (mUsed.IsUsageCalculated || mUsed.Invoices?.Count() > 0)
                {
                    throw new Exception($"Used item '{name}' cannot be deleted");
                }

                await tenantDataRepository.DeleteAsync<Used>(await Used.IdFormatAsync(nameSplit[0], year, month));
                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.Used).Name}' by name '{name}'.");
                    return NotFound(typeof(Api.Used).Name, name);
                }
                throw;
            }
        }
    }
}
