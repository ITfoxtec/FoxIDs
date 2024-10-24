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
    public class TMakeInvoiceController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public object MTenant { get; private set; }

        public TMakeInvoiceController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
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

                    mUsed.InvoiceStatus = UsedInvoiceStatus.InvoiceInitiated;
                    await tenantDataRepository.UpdateAsync(mUsed);

                    try
                    {
                        // todo

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
                        !(mUsed.PaymentStatus == UsedPaymentStatus.None || mUsed.PaymentStatus == UsedPaymentStatus.PaymentFailed))
                    {
                        throw new Exception($"Usage invoice status '{mUsed.InvoiceStatus}' is invalid, unable to send credit note.");
                    }

                    mUsed.InvoiceStatus = UsedInvoiceStatus.CreditNoteInitiated;
                    await tenantDataRepository.UpdateAsync(mUsed);

                    try
                    {
                        // todo

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
                    return NotFound(typeof(Api.Tenant).Name, $"{makeInvoiceRequest.TenantName}/{makeInvoiceRequest.Year}/{makeInvoiceRequest.Month}");
                }
                throw;
            }
        }
    }
}
