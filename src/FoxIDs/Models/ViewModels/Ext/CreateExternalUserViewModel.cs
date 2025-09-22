using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class CreateExternalUserViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public List<DynamicElementBase> ExtElements { get; set; }
    }
}
