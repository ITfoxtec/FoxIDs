namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Classification of log entries emitted by FoxIDs.
    /// </summary>
    public enum LogItemTypes
    {
        Sequence = 3,
        Operation = 6,
        Warning = 10,
        Error = 20,
        CriticalError = 30,
        Trace = 40,
        Event = 50,
        Metric = 60
    }
}
