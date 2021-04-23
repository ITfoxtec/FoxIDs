using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class LogRequestViewModel
    {
        public string StartingTime { get; set; }

        public string Filter { get; set; }

        public bool? TraceInsteadOfEvents { get; set; }

        public LogTimeIntervals? LogTimeInterval { get; set; }
    }
}
