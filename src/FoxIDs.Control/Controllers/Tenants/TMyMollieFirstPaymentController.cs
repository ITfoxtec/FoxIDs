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
using Mollie.Api.Models.Mandate.Response.PaymentSpecificParameters;
using ITfoxtec.Identity;

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
        private readonly IMandateClient mandateClient;

        public TMyMollieFirstPaymentController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, ICustomerClient customerClient, IPaymentClient paymentClient, IMandateClient mandateClient) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.customerClient = customerClient;
            this.paymentClient = paymentClient;
            this.mandateClient = mandateClient;
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
                RedirectUrl = $"{HttpContext.GetHost()}{RouteBinding.TenantName}/tenant",
                Description = "Zero amount registration payment",
                CustomerId = mTenant.Payment.CustomerId,
                SequenceType = SequenceType.First,
                CardToken = payment.CardToken
            };

            var paymentResponse = await paymentClient.CreatePaymentAsync(paymentRequest);

            mTenant.Payment.IsActive = false;
            if (!mTenant.Payment.MandateId.IsNullOrWhiteSpace())
            {
                await RevokeMandateAsync(mTenant.Payment.CustomerId, mTenant.Payment.MandateId);
            }
            mTenant.Payment.MandateId = paymentResponse.MandateId;
            var mandateResponse = await mandateClient.GetMandateAsync(mTenant.Payment.CustomerId, mTenant.Payment.MandateId) as CreditCardMandateResponse;
            var cardExpiryDate = DateTime.Parse(mandateResponse.Details.CardExpiryDate);
            mTenant.Payment.CardHolder = mandateResponse.Details.CardHolder;
            mTenant.Payment.CardNumberInfo = mandateResponse.Details.CardNumber;
            mTenant.Payment.CardLabel = mandateResponse.Details.CardLabel;
            mTenant.Payment.CardExpiryMonth = cardExpiryDate.Month;
            mTenant.Payment.CardExpiryYear = cardExpiryDate.Year;
            await tenantDataRepository.UpdateAsync(mTenant);

            return Ok(new Api.MollieFirstPaymentResponse { Status = paymentResponse.Status, CheckoutUrl = paymentResponse.Links?.Checkout?.Href });  
        }

        private async Task RevokeMandateAsync(string customerId, string mandateId)
        {
            await mandateClient.RevokeMandate(customerId, mandateId);
        }
    }
}
