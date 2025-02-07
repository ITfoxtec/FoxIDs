using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class PasswordDElement : DynamicElementBase
    {
        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public override string DField1 { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(DField1))]
        public override string DField2 { get; set; }

        public override bool Required => true;
    }
}
