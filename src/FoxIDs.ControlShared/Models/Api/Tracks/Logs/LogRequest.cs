namespace FoxIDs.Models.Api
{
    // Used to query logs.
    public class LogRequest
    {
        /// <summary>
        /// Log request starting time in Unix time seconds. E.g. DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        /// </summary>
        public long? StartingTime { get; set; }

        public string Filter { get; set; }

        public bool? TraceInsteadOfEvents { get; set; }

        public LogTimeIntervals? LogTimeInterval { get; set; }
    }
}
