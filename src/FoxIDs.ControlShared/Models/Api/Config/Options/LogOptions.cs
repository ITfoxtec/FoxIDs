namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Logging targets available for the Control plane.
    /// </summary>
    public enum LogOptions
    {
        /// <summary>
        /// Log to standard output only.
        /// </summary>
        Stdout = 1000,
        /// <summary>
        /// Send errors to OpenSearch and mirror to stdout.
        /// </summary>
        OpenSearchAndStdoutErrors = 1010,
        /// <summary>
        /// Send telemetry to Application Insights.
        /// </summary>
        ApplicationInsights = 1100,
    }
}
