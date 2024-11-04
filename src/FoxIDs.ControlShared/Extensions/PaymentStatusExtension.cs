using FoxIDs.Models.Api;

namespace FoxIDs
{
    public static class PaymentStatusExtension
    {
        public static bool PaymentApiStatusIsGenerallyFailed(this UsagePaymentStatus status)
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
