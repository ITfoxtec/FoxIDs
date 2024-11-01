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
using FoxIDs.Logic.Usage;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class TMakeInvoiceController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly InvoiceLogic invoiceLogic;

        public object MTenant { get; private set; }

        public TMakeInvoiceController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, InvoiceLogic invoiceLogic) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.invoiceLogic = invoiceLogic;
        }

        /// <summary>
        /// Create and send invoice / credit note.
        /// </summary>
        /// <param name="makeInvoiceRequest">Invoice request.</param>
        /// <returns>Used.</returns>
        [ProducesResponseType(typeof(Api.Used), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Used>> PostMakeInvoice([FromBody] Api.MakeInvoiceRequest makeInvoiceRequest)
        {
            if (settings.Payment?.EnablePayment != true || settings.Usage?.EnableInvoice != true)
            {
                throw new Exception("Payment not configured.");
            }

            try
            {
                if (!await ModelState.TryValidateObjectAsync(makeInvoiceRequest)) return BadRequest(ModelState);
                makeInvoiceRequest.TenantName = makeInvoiceRequest.TenantName.ToLower();

                var mUsed = await tenantDataRepository.GetAsync<Used>(await Used.IdFormatAsync(makeInvoiceRequest.TenantName, makeInvoiceRequest.Year, makeInvoiceRequest.Month));

                if(!makeInvoiceRequest.IsCreditNote)
                {
                    if(!(mUsed.InvoiceStatus == UsedInvoiceStatus.None || mUsed.InvoiceStatus == UsedInvoiceStatus.InvoiceFailed || mUsed.InvoiceStatus == UsedInvoiceStatus.CreditNoteSend))
                    {
                        throw new Exception($"Usage invoice status '{mUsed.InvoiceStatus}' is invalid, unable to send invoice.");
                    }

                    try
                    {
                        await invoiceLogic.CreateAndSendInvoiceAsync(mUsed);

                        mUsed.InvoiceStatus = UsedInvoiceStatus.InvoiceSend;
                        await tenantDataRepository.UpdateAsync(mUsed);

                        return Ok(mapper.Map<Api.Used>(mUsed));
                    }
                    catch
                    {
                        mUsed.InvoiceStatus = UsedInvoiceStatus.InvoiceFailed;
                        await tenantDataRepository.UpdateAsync(mUsed);
                        throw;
                    }
                }
                else
                {
                    if (!(mUsed.InvoiceStatus == UsedInvoiceStatus.InvoiceSend || mUsed.InvoiceStatus == UsedInvoiceStatus.CreditNoteFailed) ||
                        !(mUsed.PaymentStatus == UsedPaymentStatus.None || mUsed.PaymentStatus.PaymentStatusIsGenerallyFailed()))
                    {
                        throw new Exception($"Usage invoice status '{mUsed.InvoiceStatus}' is invalid, unable to send credit note.");
                    }

                    try
                    {
                        await invoiceLogic.CreateAndSendCreditNoteAsync(mUsed);

                        mUsed.InvoiceStatus = UsedInvoiceStatus.CreditNoteSend;
                        await tenantDataRepository.UpdateAsync(mUsed);

                        return Ok(mapper.Map<Api.Used>(mUsed));
                    }
                    catch
                    {
                        mUsed.InvoiceStatus = UsedInvoiceStatus.CreditNoteFailed;
                        await tenantDataRepository.UpdateAsync(mUsed);
                        throw;
                    }
                }
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.Used).Name}' by tenant name '{makeInvoiceRequest.TenantName}', year '{makeInvoiceRequest.Year}' and month '{makeInvoiceRequest.Month}'.");
                    return NotFound(typeof(Api.Used).Name, $"{makeInvoiceRequest.TenantName}/{makeInvoiceRequest.Year}/{makeInvoiceRequest.Month}");
                }
                throw;
            }
        }
    }
}
