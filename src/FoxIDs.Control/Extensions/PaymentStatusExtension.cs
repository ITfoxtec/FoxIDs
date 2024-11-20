using FoxIDs.Models;
using Mollie.Api.Models.Payment;
using System;

namespace FoxIDs
{
    public static class PaymentStatusExtension
    {
        public static UsagePaymentStatus FromMollieStatusToPaymentStatus(this string mollieStatus)
        {
            switch (mollieStatus)
            {
                case PaymentStatus.Open:
                    return UsagePaymentStatus.Open;
                case PaymentStatus.Pending:
                    return UsagePaymentStatus.Pending;
                case PaymentStatus.Authorized:
                    return UsagePaymentStatus.Authorized;
                case PaymentStatus.Paid:
                    return UsagePaymentStatus.Paid;
                case PaymentStatus.Canceled:
                    return UsagePaymentStatus.Canceled;
                case PaymentStatus.Expired:
                    return UsagePaymentStatus.Expired;
                case PaymentStatus.Failed:
                    return UsagePaymentStatus.Failed;

                default:
                    throw new NotSupportedException();
            }
        }

        public static bool PaymentStatusIsGenerallyFailed(this UsagePaymentStatus status)
        {
            switch (status)
            {
                case UsagePaymentStatus.Canceled:
                case UsagePaymentStatus.Expired:
                case UsagePaymentStatus.Failed:
                    return true;
                default:
                    return false;
            }
        }
    }
}
