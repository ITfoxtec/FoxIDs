using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.External
{
    public class ErrorResponse
    {
        [Required]
        public string Error { get; set; }
        public string ErrorDescription { get; set; }
    }
}
