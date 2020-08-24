using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class TestRiskPasswordViewModel
    {
        [Required]
        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool? IsValid { get; set; }
    }
}
