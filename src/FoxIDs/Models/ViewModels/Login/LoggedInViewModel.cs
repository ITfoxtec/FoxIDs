using System.Collections.Generic;

namespace FoxIDs.Models.ViewModels
{
    public class LoggedInViewModel : ViewModel
    {
        public List<DynamicElementBase> Elements { get; set; } = new List<DynamicElementBase>();
    }
}
