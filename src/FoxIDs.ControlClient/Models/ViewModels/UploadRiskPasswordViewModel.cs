using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class UploadRiskPasswordViewModel
    {
        public string File { get; set; }

        [Display(Name = "Upload count")]
        public int UploadCount { get; set; }
    }
}
