using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class PhoneIdentifierViewModel 
    {
        [Display(Name = "Phone")]
        [Required]
        [MaxLength(Constants.Models.User.PhoneLength)]
        [RegularExpression(Constants.Models.User.PhoneRegExPattern, ErrorMessage = "The Phone format is invalid, include your country code e.g. +44XXXXXXXXX")]
        public string Phone { get; set; }
    }
}
