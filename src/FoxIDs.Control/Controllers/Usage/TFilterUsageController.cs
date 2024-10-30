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
using Mollie.Api.Client.Abstract;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class TFilterUsageController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly IPaymentClient paymentClient;

        public TFilterUsageController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, IPaymentClient paymentClient) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.paymentClient = paymentClient;
        }

        /// <summary>
        /// Filter Usage.
        /// </summary>
        /// <param name="filterTenantName">Filter by tenant name.</param>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <returns>Tenants.</returns>
        [ProducesResponseType(typeof(HashSet<Api.Tenant>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HashSet<Api.UsedBase>>> GetFilterUsage(string filterTenantName, int year, int month)
        {
            try
            {
                if (year <= 0 || month <= 0)
                {
                    var now = DateTimeOffset.Now;
                    year = now.Year;
                    month = now.Month;
                }

                (var mUsedList, _) = filterTenantName.IsNullOrWhiteSpace() ? 
                    await tenantDataRepository.GetListAsync<Used>(whereQuery: u => u.PeriodMonth == month && u.PeriodYear == year) :
                    await tenantDataRepository.GetListAsync<Used>(whereQuery: u => u.PeriodMonth == month && u.PeriodYear == year && u.TenantName.Contains(filterTenantName, StringComparison.CurrentCultureIgnoreCase));

                var aTenants = new HashSet<Api.UsedBase>(mUsedList.Count());
                foreach (var mUsed in mUsedList.OrderBy(t => t.TenantName))
                {
                    if(!mUsed.PaymentId.IsNullOrEmpty() &&
                       (mUsed.PaymentStatus == UsedPaymentStatus.Open || mUsed.PaymentStatus == UsedPaymentStatus.Pending || mUsed.PaymentStatus == UsedPaymentStatus.Authorized))
                    {
                        var paymentResponse = await paymentClient.GetPaymentAsync(mUsed.PaymentId);
                        mUsed.PaymentStatus = paymentResponse.Status.FromMollieStatusToPaymentStatus();
                        await tenantDataRepository.UpdateAsync(mUsed);
                    }
                    aTenants.Add(mapper.Map<Api.UsedBase>(mUsed));
                }
                return Ok(aTenants);
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
