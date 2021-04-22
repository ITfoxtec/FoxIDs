namespace FoxIDs.Models.Api
{
    // Used to query logs.
    public class LogRequest
    {
        public string Filter { get; set; }

        public bool TraceInsteadOfEvents { get; set; }

        public string StartingTime { get; set; }

        public LogTimeIntervals LogTimeInterval { get; set; }
    }
}
