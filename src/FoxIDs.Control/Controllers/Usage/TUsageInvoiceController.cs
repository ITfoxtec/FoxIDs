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
    public class TUsageInvoiceController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public object MTenant { get; private set; }

        public TUsageInvoiceController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Create and send invoice / credit note.
        /// </summary>
        /// <param name="usageInvoiceRequest">Invoice request.</param>
        /// <returns>Used.</returns>
        [ProducesResponseType(typeof(Api.Used), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Used>> PutUsageInvoiceController([FromBody] Api.UsageInvoiceRequest usageInvoiceRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(usageInvoiceRequest)) return BadRequest(ModelState);
                usageInvoiceRequest.TenantName = usageInvoiceRequest.TenantName.ToLower();

                var mUsed = await tenantDataRepository.GetAsync<Used>(await Used.IdFormatAsync(usageInvoiceRequest.TenantName, usageInvoiceRequest.Year, usageInvoiceRequest.Month));

                if(!usageInvoiceRequest.IsCreditNote)
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
                    if (!(mUsed.InvoiceStatus == UsedInvoiceStatus.InvoiceSend || mUsed.InvoiceStatus == UsedInvoiceStatus.CreditNoteFailed))
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
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.Used).Name}' by tenant name '{usageInvoiceRequest.TenantName}', year '{usageInvoiceRequest.Year}' and month '{usageInvoiceRequest.Month}'.");
                    return NotFound(typeof(Api.Tenant).Name, $"{usageInvoiceRequest.TenantName}/{usageInvoiceRequest.Year}/{usageInvoiceRequest.Month}");
                }
                throw;
            }
        }
    }
}
