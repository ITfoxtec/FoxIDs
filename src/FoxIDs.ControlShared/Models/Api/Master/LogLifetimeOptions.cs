namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Available retention windows for stored logs.
    /// </summary>
    public enum LogLifetimeOptions
    {
        /// <summary>
        /// Retain logs for up to 30 days.
        /// </summary>
        Max30Days = 30,
        /// <summary>
        /// Retain logs for up to 180 days.
        /// </summary>
        Max180Days = 180,
    }
}
