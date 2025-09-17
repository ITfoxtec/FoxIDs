using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class LogoutViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        [Required]
        public LogoutChoice LogoutChoice { get; set; }

        public List<DynamicElementBase> Elements { get; set; } = new List<DynamicElementBase>();
    }
}
