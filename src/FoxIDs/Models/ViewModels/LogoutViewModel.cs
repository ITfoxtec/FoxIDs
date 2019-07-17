using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class LogoutViewModel
    {
        public string SequenceString { get; set; }

        [Required]
        public LogoutChoice LogoutChoice { get; set; }
    }
}
