using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterSmsPriceViewModel
    {
        /// <summary>
        /// Search by country name and ISO2.
        /// </summary>
        [MaxLength(Constants.Models.SmsPrices.CountryNameLength)]
        [Display(Name = "Search SMS price")]
        public string FilterName { get; set; }
    }
}
