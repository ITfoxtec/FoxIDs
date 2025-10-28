using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
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
using FoxIDs.Models;
using FoxIDs.Logic.Usage;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class TUsagesController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly UsageInvoicingLogic usageInvoicingLogic;

        public TUsagesController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, UsageInvoicingLogic usageInvoicingLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.usageInvoicingLogic = usageInvoicingLogic;
        }

        /// <summary>
        /// Get Usages.
        /// </summary>
        /// <param name="filterTenantName">Filter by tenant name.</param>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <param name="paginationToken">The pagination token.</param>
        /// <returns>Tenants.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.UsedBase>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Api.PaginationResponse<Api.UsedBase>>> GetUsages(string filterTenantName, int year, int month, string paginationToken = null)
        {
            try
            {
                filterTenantName = filterTenantName?.Trim();
                if (year <= 0 || month <= 0)
                {
                    var now = DateTimeOffset.Now;
                    year = now.Year;
                    month = now.Month;
                }

                (var mUsedList, var nextPaginationToken) = filterTenantName.IsNullOrWhiteSpace() ? 
                    await tenantDataRepository.GetManyAsync<Used>(whereQuery: u => u.PeriodEndDate.Month == month && u.PeriodEndDate.Year == year, paginationToken: paginationToken) :
                    await tenantDataRepository.GetManyAsync<Used>(whereQuery: u => u.PeriodEndDate.Month == month && u.PeriodEndDate.Year == year && u.TenantName.Contains(filterTenantName, StringComparison.CurrentCultureIgnoreCase), paginationToken: paginationToken);

                var response = new Api.PaginationResponse<Api.UsedBase>
                {
                    Data = new HashSet<Api.UsedBase>(mUsedList.Count()),
                    PaginationToken = nextPaginationToken,
                };
                foreach (var mUsed in mUsedList.OrderBy(t => t.TenantName))
                {
                    await usageInvoicingLogic.UpdatePaymentAndSendInvoiceAsync(mUsed);

                    var aUsed = mapper.Map<Api.UsedBase>(mUsed);
                    var mLastInvoice = mUsed.Invoices?.LastOrDefault();
                    if(mLastInvoice != null)
                    {
                        var aLastInvoice = mapper.Map<Api.Invoice>(mLastInvoice);
                        aLastInvoice.Lines = null;
                        aLastInvoice.TimeItems = null;
                        aUsed.Invoices = [aLastInvoice];

                    }
                    response.Data.Add(aUsed);
                }
                return Ok(response);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.Used).Name}' by filter tenant name '{filterTenantName}'.");
                    return NotFound(typeof(Api.Used).Name, filterTenantName);
                }
                throw;
            }
        }
    }
}
