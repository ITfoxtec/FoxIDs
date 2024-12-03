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
using System.Linq;
using System.Threading;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class TUsageInvoicingActionController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly UsageInvoicingLogic usageInvoicingLogic;
        private readonly UsageMolliePaymentLogic usageMolliePaymentLogic;

        public object MTenant { get; private set; }

        public TUsageInvoicingActionController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, UsageInvoicingLogic usageInvoicingLogic, UsageMolliePaymentLogic usageMolliePaymentLogic) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.usageInvoicingLogic = usageInvoicingLogic;
            this.usageMolliePaymentLogic = usageMolliePaymentLogic;
        }

        /// <summary>
        /// Execute a usage invoicing action.
        /// </summary>
        /// <param name="action">Usage invoicing action.</param>
        /// <returns>Used.</returns>
        [ProducesResponseType(typeof(Api.Used), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Used>> PostUsageInvoicingAction([FromBody] Api.UsageInvoicingAction action)
        {
            if (settings.Payment?.EnablePayment != true || settings.Usage?.EnableInvoice != true)
            {
                throw new Exception("Payment not configured.");
            }

            try
            {
                if (!await ModelState.TryValidateObjectAsync(action)) return BadRequest(ModelState);
                action.TenantName = action.TenantName.ToLower();

                var mUsed = await tenantDataRepository.GetAsync<Used>(await Used.IdFormatAsync(action.TenantName, action.PeriodBeginDate.Year, action.PeriodBeginDate.Month));

                if (action.DoInvoicing)
                {
                    await DoInvoicing(mUsed);
                }
                else if (action.DoSendInvoiceAgain)
                {
                    await DoSendInvoiceAgain(mUsed);
                }
                else if (action.DoCreditNote)
                {
                    await DoCreditNote(mUsed);
                }
                else if (action.DoSendCreditNoteAgain)
                {
                    await DoSendCreditNoteAgain(mUsed);
                }
                else if (action.DoPaymentAgain)
                {
                    await DoPaymentAgain(mUsed);
                }
                else if (action.MarkAsPaid)
                {
                    await MarkAsPaid(mUsed);
                }
                else if (action.MarkAsNotPaid)
                {
                    await MarkAsNotPaid(mUsed);
                }
                else
                {
                    throw new InvalidOperationException();
                }

                mUsed = await tenantDataRepository.GetAsync<Used>(await Used.IdFormatAsync(action.TenantName, action.PeriodBeginDate.Year, action.PeriodBeginDate.Month));
                return Ok(mapper.Map<Api.Used>(mUsed));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Do '{typeof(Api.Used).Name}' action by tenant name '{action.TenantName}', year '{action.PeriodBeginDate.Year}' and month '{action.PeriodBeginDate.Month}'.");
                    return NotFound(typeof(Api.Used).Name, $"{action.TenantName}/{action.PeriodBeginDate.Year}/{action.PeriodBeginDate.Month}");
                }
                throw;
            }
        }

        private async Task DoInvoicing(Used mUsed)
        {
            var mTenant = await tenantDataRepository.GetAsync<Tenant>(await Tenant.IdFormatAsync(mUsed.TenantName));

            if (mTenant.EnableUsage == true)
            {
                if ((mUsed.IsUsageCalculated || mUsed.Items?.Count() > 0) && !mUsed.IsDone)
                {
                    using var cancellationTokenSource = new CancellationTokenSource();
                    await usageInvoicingLogic.DoInvoicingAsync(mTenant, mUsed, cancellationTokenSource.Token);
                }
                else
                {
                    throw new Exception("The usage is not calculated or no items exits or it is already done and can not be invoiced.");
                }
            }
            else
            {
                if(mUsed.IsDone)
                {
                    throw new Exception("The usage is already done and can not be invoiced.");
                }
                else if (mUsed.Items?.Count() > 0)
                {
                    using var cancellationTokenSource = new CancellationTokenSource();
                    await usageInvoicingLogic.DoInvoicingAsync(mTenant, mUsed, cancellationTokenSource.Token, doInvoicing: true);
                }
                else
                {
                    logger.Event($"Usage, no items to invoice for tenant '{mUsed.TenantName}'.");
                }
            }
        }

        private async Task DoSendInvoiceAgain(Used mUsed)
        {
            if (mUsed.IsInvoiceReady)
            {
                var mTenant = await tenantDataRepository.GetAsync<Tenant>(await Tenant.IdFormatAsync(mUsed.TenantName));
                var invoice = mUsed.Invoices.Last();
                invoice.Customer = mTenant.Customer;
                if(!invoice.IsCardPayment || mUsed.PaymentStatus == UsagePaymentStatus.Paid)
                {
                    await usageInvoicingLogic.SendInvoiceAsync(mUsed, invoice);
                }
                else
                {
                    throw new Exception("The invoice is not paid and is not ready and can not be send again.");
                }
            }
            else
            {
                throw new Exception("The invoice is not ready and can not be send again.");
            }
        }

        private async Task DoCreditNote(Used mUsed)
        {
            if (mUsed.IsInvoiceReady && mUsed.PaymentStatus == UsagePaymentStatus.None || mUsed.PaymentStatus.PaymentStatusIsGenerallyFailed())
            {
                await usageInvoicingLogic.CreateAndSendCreditNoteAsync(mUsed);
            }
            else
            {
                throw new Exception("The invoice is not ready and a credit note can not be send.");
            }
        }

        private async Task DoSendCreditNoteAgain(Used mUsed)
        {
            var invoice = mUsed.Invoices.LastOrDefault();
            if (!mUsed.IsInvoiceReady && invoice?.IsCreditNote == true)
            {
                await usageInvoicingLogic.SendInvoiceAsync(mUsed, invoice);
            }
            else
            {
                throw new Exception("There is not already a credit note which can be send again.");
            }
        }

        private async Task DoPaymentAgain(Used mUsed)
        {
            if (mUsed.IsInvoiceReady)
            {
                if (mUsed.PaymentStatus.PaymentStatusIsGenerallyFailed())
                {
                    var mTenant = await tenantDataRepository.GetAsync<Tenant>(await Tenant.IdFormatAsync(mUsed.TenantName));
                    var invoice = mUsed.Invoices.Last();
                    await usageMolliePaymentLogic.DoPaymentAsync(mTenant, mUsed, invoice);
                }
                else
                {
                    throw new Exception($"There is payment with status {mUsed.PaymentStatus} and payment is not possible.");
                }
            }
            else
            {
                throw new Exception("The invoice is not ready and payment is not possible.");
            }
        }   
        
        private async Task MarkAsPaid(Used mUsed)
        {
            if (mUsed.IsInvoiceReady)
            {
                await usageMolliePaymentLogic.MarkAsPaidAsync(mUsed);
            }
            else
            {
                throw new Exception("The invoice is not ready and it is not possible to make as paid.");
            }
        }

        private async Task MarkAsNotPaid(Used mUsed)
        {
            if (mUsed.IsInvoiceReady && mUsed.PaymentStatus == UsagePaymentStatus.Paid)
            {
                await usageMolliePaymentLogic.MarkAsNotPaidAsync(mUsed);
            }
            else
            {
                throw new Exception("The invoice is not ready and it is not possible to make as paid.");
            }
        }
    }
}
