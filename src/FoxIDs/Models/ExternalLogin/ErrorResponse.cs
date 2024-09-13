using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ExternalLogin
{
    public class ErrorResponse
    {
        [Required]
        public string Error { get; set; }
        public string ErrorDescription { get; set; }
    }
}
