using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public class LogResponseViewModel
    {
        public List<LogItemViewModel> Items { get; set; }

        public bool ResponseTruncated { get; set; }
    }
}
