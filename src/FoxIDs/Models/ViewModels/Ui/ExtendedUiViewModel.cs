using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class ExtendedUiViewModel : ViewModel
    {
        public string PageTitle { get; set; }

        public string SubmitButtonText { get; set; }

        public string SequenceString { get; set; }

        [Required]
        public string State { get; set; }

        public List<DynamicElementBase> ExtendedUiElements { get; set; }

        public List<DynamicElementBase> Elements { get; set; }
    }
}