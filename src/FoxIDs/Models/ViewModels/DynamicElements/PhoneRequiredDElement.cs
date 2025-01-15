using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class PhoneRequiredDElement : PhoneDElement
    {
        [Required]
        [MaxLength(Constants.Models.User.PhoneLength)]
        [RegularExpression(Constants.Models.User.PhoneRegExPattern, ErrorMessage = "The Phone format is invalid, include your country code e.g. +44XXXXXXXXX")]
        [Display(Name = "Phone")]
        public override string DField1 { get; set; }

        public override bool Required => true;
    }
}
