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
    public class TMakePaymentController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public object MTenant { get; private set; }

        public TMakePaymentController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
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
            try
            {
                if (!await ModelState.TryValidateObjectAsync(makePaymentRequest)) return BadRequest(ModelState);
                makePaymentRequest.TenantName = makePaymentRequest.TenantName.ToLower();

                var mUsed = await tenantDataRepository.GetAsync<Used>(await Used.IdFormatAsync(makePaymentRequest.TenantName, makePaymentRequest.Year, makePaymentRequest.Month));

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
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.Used).Name}' by tenant name '{makePaymentRequest.TenantName}', year '{makePaymentRequest.Year}' and month '{makePaymentRequest.Month}'.");
                    return NotFound(typeof(Api.Tenant).Name, $"{makePaymentRequest.TenantName}/{makePaymentRequest.Year}/{makePaymentRequest.Month}");
                }
                throw;
            }
        }
    }
}
