namespace FoxIDs.Models.Api
{
    public class Payment
    {
        public bool IsActive { get; set; }

        public string CardHolder { get; set; }

        public string CardNumber { get; set; }

        public string CardLabel { get; set; }

        public int CardExpiryMonth { get; set; }

        public int CardExpiryYear { get; set; }
    }
}
