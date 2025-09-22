using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class CreateUserViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public List<DynamicElementBase> CreateUserElements { get; set; }

        public List<DynamicElementBase> Elements { get; set; }
    }
}
