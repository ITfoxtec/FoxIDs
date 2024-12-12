using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models.Config;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Client;
using Mollie.Api.Models;
using Mollie.Api.Models.Payment;
using Mollie.Api.Models.Customer.Request;
using Mollie.Api.Models.Payment.Request.PaymentSpecificParameters;
using ITfoxtec.Identity;
using FoxIDs.Logic.Usage;
using FoxIDs.Logic.Logs;

namespace FoxIDs.Controllers
{
    /// <summary>
    /// Register first Mollie payment.
    /// </summary>
    [TenantScopeAuthorize]
    public class TMyMollieFirstPaymentController :  ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly ICustomerClient customerClient;
        private readonly IPaymentClient paymentClient;
        private readonly UsageMolliePaymentLogic usageMolliePaymentLogic;
        private readonly SendEventEmailLogic sendEventEmailLogic;

        public TMyMollieFirstPaymentController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, ICustomerClient customerClient, IPaymentClient paymentClient, UsageMolliePaymentLogic usageMolliePaymentLogic, SendEventEmailLogic sendEventEmailLogic) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.customerClient = customerClient;
            this.paymentClient = paymentClient;
            this.usageMolliePaymentLogic = usageMolliePaymentLogic;
            this.sendEventEmailLogic = sendEventEmailLogic;
        }

        /// <summary>
        /// Register first Mollie payment.
        /// </summary>
        /// <param name="payment">First payment.</param>
        /// <returns>Tenant.</returns>
        [ProducesResponseType(typeof(Api.MollieFirstPaymentResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.MollieFirstPaymentResponse>> PostMyMollieFirstPayment([FromBody] Api.MollieFirstPaymentRequest payment)
        {
            if(settings.Payment?.EnablePayment != true || settings.Usage?.EnableInvoice != true)
            {
                throw new Exception("Payment not configured.");
            }

            var mTenant = await tenantDataRepository.GetTenantByNameAsync(RouteBinding.TenantName);
            if(string.IsNullOrWhiteSpace(mTenant.Payment?.CustomerId))
            {
                try
                {
                    var customerResponse = await customerClient.CreateCustomerAsync(new CustomerRequest()
                    {
                        Name = RouteBinding.TenantName
                    });

                    mTenant.Payment = new Payment
                    {
                        CustomerId = customerResponse.Id
                    };
                    await tenantDataRepository.UpdateAsync(mTenant);
                }
                catch (MollieApiException ex)
                {
                    logger.Error(ex, "Create Mollie customer error.");
                    throw new Exception("Unable to create customer in Mollie.");
                }
            }

            var paymentRequest = new CreditCardPaymentRequest
            {
                Amount = new Amount("EUR", "0.00"),
                RedirectUrl = $"{HttpContext.GetHost()}{RouteBinding.TenantName}/tenantpaymentresponse",
                Description = "Zero amount registration payment",
                CustomerId = mTenant.Payment.CustomerId,
                SequenceType = SequenceType.First,
                CardToken = payment.CardToken
            };

            var paymentResponse = await paymentClient.CreatePaymentAsync(paymentRequest);

            mTenant.Payment.IsActive = false;
            if (!mTenant.Payment.MandateId.IsNullOrWhiteSpace())
            {
                await usageMolliePaymentLogic.RevokePaymentMandateAsync(mTenant);
            }
            mTenant.Payment.MandateId = paymentResponse.MandateId;
            await tenantDataRepository.UpdateAsync(mTenant);

            await sendEventEmailLogic.SendEventEmailAsync($"Payment card added - '{mTenant.Name}'.", $"Payment card added to tenant '{mTenant.Name}'. Plan: '{mTenant.PlanName}', Enable usage: '{mTenant.EnableUsage}', Do payment: '{mTenant.DoPayment}'.");

            await usageMolliePaymentLogic.UpdatePaymentMandate(mTenant);

            return Ok(new Api.MollieFirstPaymentResponse { CheckoutUrl = paymentResponse.Links?.Checkout?.Href });  
        }
    }
}
