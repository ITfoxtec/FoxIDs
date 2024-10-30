using FoxIDs.Models;
using Mollie.Api.Models.Payment;
using System;

namespace FoxIDs
{
    public static class PaymentStatusExtension
    {
        public static UsedPaymentStatus FromMollieStatusToPaymentStatus(this string mollieStatus)
        {
            switch (mollieStatus)
            {
                case PaymentStatus.Open:
                    return UsedPaymentStatus.Open;
                case PaymentStatus.Pending:
                    return UsedPaymentStatus.Pending;
                case PaymentStatus.Authorized:
                    return UsedPaymentStatus.Authorized;
                case PaymentStatus.Paid:
                    return UsedPaymentStatus.Paid;
                case PaymentStatus.Canceled:
                    return UsedPaymentStatus.Canceled;
                case PaymentStatus.Expired:
                    return UsedPaymentStatus.Expired;
                case PaymentStatus.Failed:
                    return UsedPaymentStatus.Failed;

                default:
                    throw new NotSupportedException();
            }
        }

        public static bool PaymentStatusIsGenerallyFailed(this UsedPaymentStatus status)
        {
            switch (status)
            {
                case UsedPaymentStatus.Canceled:
                case UsedPaymentStatus.Expired:
                case UsedPaymentStatus.Failed:
                    return true;
                default:
                    return false;
            }
        }
    }
}
