using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class CreateExternalUserViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        [Required]
        public List<DynamicElementBase> Elements { get; set; }
    }
}
