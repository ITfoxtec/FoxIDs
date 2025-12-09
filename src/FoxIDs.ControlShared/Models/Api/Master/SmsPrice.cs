using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Pricing information for sending SMS messages to a country.
    /// </summary>
    public class SmsPrice
    {
        //[Required]
        /// <summary>
        /// Country name used for display purposes.
        /// </summary>
        [MaxLength(Constants.Models.SmsPrices.CountryNameLength)]
        [Display(Name = "Country name")]
        public string CountryName { get; set; }

        /// <summary>
        /// ISO2 country code.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.SmsPrices.Iso2Length)]
        [Display(Name = "ISO2")]
        public string Iso2 { get; set; }

        /// <summary>
        /// Telephone country code.
        /// </summary>
        [Required]
        [Min(Constants.Models.SmsPrices.PhoneCodeMin)]
        [Display(Name = "Phone code")]
        public int PhoneCode { get; set; }

        /// <summary>
        /// Price per SMS in EUR.
        /// </summary>
        [Required]
        [Min(Constants.Models.SmsPrices.PriceMin)]
        [Display(Name = "Price")]
        public decimal Price { get; set; }
    }
}
