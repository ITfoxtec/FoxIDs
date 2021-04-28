using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public class LogItemDetailViewModel
    {
        public string Name { get; set; }

        public List<string> Details { get; set; }

        public bool ShowDetails { get; set; }
    }
}
