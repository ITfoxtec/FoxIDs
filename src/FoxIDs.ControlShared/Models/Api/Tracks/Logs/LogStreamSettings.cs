namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Log stream settings in track.
    /// </summary>
    public class LogStreamSettings : LogSettings
    {
        public bool LogWarning { get; set; }

        public bool LogError { get; set; }

        public bool LogCriticalError { get; set; }

        public bool LogEvent { get; set; }
    }
}
