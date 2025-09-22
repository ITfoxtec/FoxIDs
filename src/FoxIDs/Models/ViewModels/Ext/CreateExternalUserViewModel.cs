using System.Collections.Generic;

namespace FoxIDs.Models.ViewModels
{
    public class CreateExternalUserViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public List<DynamicElementBase> ExtElements { get; set; }

        public List<DynamicElementBase> Elements { get; set; }
    }
}