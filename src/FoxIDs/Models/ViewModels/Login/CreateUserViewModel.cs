using System.Collections.Generic;

namespace FoxIDs.Models.ViewModels
{
    public class CreateUserViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public List<DynamicElementBase> InputElements { get; set; }

        public List<DynamicElementBase> Elements { get; set; }
    }
}
