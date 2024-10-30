using FoxIDs.Models.Api;

namespace FoxIDs
{
    public static class PaymentStatusExtension
    {
        public static bool PaymentApiStatusIsGenerallyFailed(this UsedPaymentStatus status)
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
