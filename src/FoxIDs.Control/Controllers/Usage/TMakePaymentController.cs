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
using FoxIDs.Models.Config;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models.Payment;
using Mollie.Api.Models;
using System.Linq;
using Mollie.Api.Models.Payment.Request;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class TMakePaymentController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly IPaymentClient paymentClient;

        public TMakePaymentController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, IPaymentClient paymentClient) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.paymentClient = paymentClient;
        }

        /// <summary>
        /// Make payment.
        /// </summary>
        /// <param name="makePaymentRequest">Payment request.</param>
        /// <returns>Used.</returns>
        [ProducesResponseType(typeof(Api.Used), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Used>> PostMakePayment([FromBody] Api.MakePaymentRequest makePaymentRequest)
        {
            if (settings.Payment?.EnablePayment != true || settings.Usage?.EnableInvoice != true)
            {
                throw new Exception("Payment not configured.");
            }

            try
            {
                if (!await ModelState.TryValidateObjectAsync(makePaymentRequest)) return BadRequest(ModelState);
                makePaymentRequest.TenantName = makePaymentRequest.TenantName.ToLower();

                var mUsed = await tenantDataRepository.GetAsync<Used>(await Used.IdFormatAsync(makePaymentRequest.TenantName, makePaymentRequest.PeriodYear, makePaymentRequest.PeriodMonth));

                if(!((mUsed.InvoiceStatus == UsedInvoiceStatus.InvoiceSend || mUsed.InvoiceStatus == UsedInvoiceStatus.CreditNoteFailed) && 
                     (mUsed.PaymentStatus == UsedPaymentStatus.None || mUsed.PaymentStatus.PaymentStatusIsGenerallyFailed())))
                {
                    throw new Exception($"Usage invoice status '{mUsed.InvoiceStatus}' and payment status '{mUsed.PaymentStatus}' is invalid, unable to execute payment.");
                }

                await tenantDataRepository.UpdateAsync(mUsed);

                try
                {
                    await MakePaymentAsync(mUsed);
                    await tenantDataRepository.UpdateAsync(mUsed);

                    return Ok(mapper.Map<Api.Used>(mUsed));
                }
                catch
                {
                    mUsed.PaymentStatus = UsedPaymentStatus.Failed;
                    await tenantDataRepository.UpdateAsync(mUsed);
                    throw;
                }
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.Used).Name}' by tenant name '{makePaymentRequest.TenantName}', year '{makePaymentRequest.PeriodYear}' and month '{makePaymentRequest.PeriodMonth}'.");
                    return NotFound(typeof(Api.Tenant).Name, $"{makePaymentRequest.TenantName}/{makePaymentRequest.PeriodYear}/{makePaymentRequest.PeriodMonth}");
                }
                throw;
            }
        }

        private async Task MakePaymentAsync(Used mUsed)
        {
            var mTenant = await tenantDataRepository.GetTenantByNameAsync(mUsed.TenantName);
            if(mTenant.Payment?.IsActive != true)
            {
                throw new Exception("Not an active payment.");
            }

            var invoice = mUsed.Invoices.Where(i => !i.IsCreditNote).Last();

            var paymentRequest = new PaymentRequest
            {
                RedirectUrl = "https://www.foxids.com",
                Amount = new Amount("EUR", invoice.TotalPrice),
                Description = "FoxIDs subscription",
                CustomerId = mTenant.Payment.CustomerId,
                SequenceType = SequenceType.Recurring,
                MandateId = mTenant.Payment.MandateId,
            };

            var paymentResponse = await paymentClient.CreatePaymentAsync(paymentRequest);
            mUsed.PaymentStatus = paymentResponse.Status.FromMollieStatusToPaymentStatus();
            mUsed.PaymentId = paymentResponse.Id;
        }
    }
}
