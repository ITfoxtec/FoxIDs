using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class TestRiskPasswordViewModel
    {
        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool? IsValid { get; set; }
    }
}
