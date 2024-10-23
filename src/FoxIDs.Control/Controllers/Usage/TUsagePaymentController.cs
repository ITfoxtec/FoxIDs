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

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class TUsagePaymentController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public object MTenant { get; private set; }

        public TUsagePaymentController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Execute payment.
        /// </summary>
        /// <param name="usagePaymentRequest">Payment request.</param>
        /// <returns>Used.</returns>
        [ProducesResponseType(typeof(Api.Used), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Used>> PutUsagePaymentController([FromBody] Api.UsageRequest usagePaymentRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(usagePaymentRequest)) return BadRequest(ModelState);
                usagePaymentRequest.TenantName = usagePaymentRequest.TenantName.ToLower();

                var mUsed = await tenantDataRepository.GetAsync<Used>(await Used.IdFormatAsync(usagePaymentRequest.TenantName, usagePaymentRequest.Year, usagePaymentRequest.Month));

                if(!((mUsed.InvoiceStatus == UsedInvoiceStatus.InvoiceSend || mUsed.InvoiceStatus == UsedInvoiceStatus.CreditNoteFailed) && 
                     (mUsed.PaymentStatus == UsedPaymentStatus.None || mUsed.PaymentStatus == UsedPaymentStatus.PaymentFailed)))
                {
                    throw new Exception($"Usage invoice status '{mUsed.InvoiceStatus}' and payment status '{mUsed.PaymentStatus}' is invalid, unable to execute payment.");
                }

                mUsed.PaymentStatus = UsedPaymentStatus.PaymentInitiated;
                await tenantDataRepository.UpdateAsync(mUsed);

                try
                {
                    // todo

                    mUsed.PaymentStatus = UsedPaymentStatus.PaymentDone;
                    await tenantDataRepository.UpdateAsync(mUsed);

                    return Ok(mapper.Map<Api.Used>(mUsed));
                }
                catch
                {
                    mUsed.PaymentStatus = UsedPaymentStatus.PaymentFailed;
                    await tenantDataRepository.UpdateAsync(mUsed);
                    throw;
                }
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.Used).Name}' by tenant name '{usagePaymentRequest.TenantName}', year '{usagePaymentRequest.Year}' and month '{usagePaymentRequest.Month}'.");
                    return NotFound(typeof(Api.Tenant).Name, $"{usagePaymentRequest.TenantName}/{usagePaymentRequest.Year}/{usagePaymentRequest.Month}");
                }
                throw;
            }
        }
    }
}
