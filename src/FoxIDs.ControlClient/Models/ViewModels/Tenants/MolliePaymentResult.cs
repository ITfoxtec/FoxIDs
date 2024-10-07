namespace FoxIDs.Client.Models.ViewModels
{
    public class MolliePaymentResult
    {
        public string Token { get; set; }  

        public MolliePaymentErrorResult Error { get; set; }

    }
}
