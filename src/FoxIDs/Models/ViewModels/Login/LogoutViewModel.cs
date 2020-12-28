using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class LogoutViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        [Required]
        public LogoutChoice LogoutChoice { get; set; }
    }
}
