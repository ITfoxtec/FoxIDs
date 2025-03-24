using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralSmsPriceViewModel : SmsPrice
    {
        public GeneralSmsPriceViewModel()
        { }

        public GeneralSmsPriceViewModel(SmsPrice smsPrice)
        {
            CountryName = smsPrice.CountryName;
            Iso2 = smsPrice.Iso2;
        }

        public bool Edit { get; set; }

        public bool CreateMode { get; set; }

        public bool DeleteAcknowledge { get; set; }

        public string Error { get; set; }

        public PageEditForm<SmsPrice> Form { get; set; }
    }
}
