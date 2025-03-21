using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class SmsPrice
    {
        //[Required]
        [MaxLength(Constants.Models.SmsPrices.CountryNameLength)]
        [Display(Name = "Country name")]
        public string CountryName { get; set; }

        [Required]
        [MaxLength(Constants.Models.SmsPrices.Iso2Length)]
        [Display(Name = "ISO2")]
        public string Iso2 { get; set; }

        [Required]
        [Min(Constants.Models.SmsPrices.PhoneCodeMin)]
        [Display(Name = "Phone code")]
        public int PhoneCode { get; set; }

        [Required]
        [Min(Constants.Models.SmsPrices.PriceMin)]
        [Display(Name = "Price")]
        public decimal Price { get; set; }
    }
}
