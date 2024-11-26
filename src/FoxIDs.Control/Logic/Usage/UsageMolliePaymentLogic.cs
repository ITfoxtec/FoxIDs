using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models;
using Mollie.Api.Models.Mandate.Response.PaymentSpecificParameters;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models.Payment;
using System;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;

namespace FoxIDs.Logic.Usage
{
    public class UsageMolliePaymentLogic
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly IMandateClient mandateClient;
        private readonly IPaymentClient paymentClient;

        public UsageMolliePaymentLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, IMandateClient mandateClient, IPaymentClient paymentClient)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.mandateClient = mandateClient;
            this.paymentClient = paymentClient;
        }

        public bool HasCardPayment(Tenant tenant)
        {
            if (tenant.Payment != null && !string.IsNullOrEmpty(tenant.Payment.MandateId))
            {
                return true;
            }
            return false;
        }

        public bool HasActiveCardPayment(Tenant tenant)
        {
            if (HasCardPayment(tenant) && tenant.Payment.IsActive)
            {
                return true;
            }
            return false;
        }

        public async Task UpdatePaymentMandate(Tenant tenant)
        {
            if (tenant.Payment != null && string.IsNullOrEmpty(tenant.Payment.CardNumberInfo) && !string.IsNullOrEmpty(tenant.Payment.MandateId))
            {
                var mandateResponse = await mandateClient.GetMandateAsync(tenant.Payment.CustomerId, tenant.Payment.MandateId) as CreditCardMandateResponse;
                if ("valid".Equals(mandateResponse.Status, StringComparison.OrdinalIgnoreCase))
                {
                    tenant.Payment.IsActive = true;
                    var cardExpiryDate = DateTime.Parse(mandateResponse.Details.CardExpiryDate);
                    tenant.Payment.CardHolder = mandateResponse.Details.CardHolder;
                    tenant.Payment.CardNumberInfo = mandateResponse.Details.CardNumber;
                    tenant.Payment.CardLabel = mandateResponse.Details.CardLabel;
                    tenant.Payment.CardExpiryMonth = cardExpiryDate.Month;
                    tenant.Payment.CardExpiryYear = cardExpiryDate.Year;

                    await tenantDataRepository.UpdateAsync(tenant);
                }
            }
        }

        public async Task RevokePaymentMandateAsync(Tenant tenant)
        {
            if (tenant.Payment != null)
            {
                await mandateClient.RevokeMandate(tenant.Payment.CustomerId, tenant.Payment.MandateId);
            }
        }


        public async Task<bool> DoPaymentAsync(Tenant tenant, Used used, Invoice invoice)
        {
            if (!HasActiveCardPayment(tenant))
            {
                throw new InvalidOperationException("Not an active payment.");
            }

            try
            {
                logger.Event($"Usage, payment 'card' for tenant '{used.TenantName}' started.");

                var paymentRequest = new PaymentRequest
                {
                    RedirectUrl = "https://www.foxids.com",
                    Amount = new Amount(invoice.Currency, invoice.TotalPrice),
                    Description = "FoxIDs subscription",
                    CustomerId = tenant.Payment.CustomerId,
                    SequenceType = SequenceType.Recurring,
                    MandateId = tenant.Payment.MandateId,
                };

                var paymentResponse = await paymentClient.CreatePaymentAsync(paymentRequest);
                used.PaymentStatus = paymentResponse.Status.FromMollieStatusToPaymentStatus();
                used.PaymentId = paymentResponse.Id;
                await tenantDataRepository.UpdateAsync(used);

                logger.Event($"Usage, payment 'card' for tenant '{used.TenantName}' status '{used.PaymentStatus}'.");

                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                try
                {
                    used.PaymentStatus = UsagePaymentStatus.Failed;
                    await tenantDataRepository.UpdateAsync(used);
                }
                catch (Exception saveEx)
                {
                    logger.Error(saveEx, $"Usage, unable to save status: {UsagePaymentStatus.Failed}.");
                }
                logger.Error(ex, $"Usage, payment 'card' for tenant '{used.TenantName}' error..");
                return false;
            }
        }

        public async Task<bool> UpdatePaymentAsync(Used used)
        {
            if (used.PaymentId.IsNullOrEmpty())
            {
                throw new InvalidOperationException("The payment id is empty.");
            }

            try
            {
                logger.Event($"Usage, read payment 'card' for tenant '{used.TenantName}' started.");

                var paymentResponse = await paymentClient.GetPaymentAsync(used.PaymentId);
                used.PaymentStatus = paymentResponse.Status.FromMollieStatusToPaymentStatus();
                await tenantDataRepository.UpdateAsync(used);

                logger.Event($"Usage, read payment 'card' for tenant '{used.TenantName}' status '{used.PaymentStatus}'.");

                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    used.PaymentStatus = UsagePaymentStatus.Failed;
                    await tenantDataRepository.UpdateAsync(used);
                }
                catch (Exception saveEx)
                {
                    logger.Error(saveEx, $"Usage, unable to save status: {UsagePaymentStatus.Failed}.");
                }
                logger.Error(ex, $"Usage, read payment 'card' for tenant '{used.TenantName}' error.");
                return false;
            }
        }

        public async Task MarkAsPaidAsync(Used used)
        {
            used.PaymentStatus = UsagePaymentStatus.Paid;
            await tenantDataRepository.UpdateAsync(used);
        }

        public async Task MarkAsNotPaidAsync(Used used)
        {
            used.PaymentStatus = UsagePaymentStatus.None;
            await tenantDataRepository.UpdateAsync(used);
        }
    }
}
