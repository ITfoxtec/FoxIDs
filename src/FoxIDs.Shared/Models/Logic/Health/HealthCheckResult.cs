using Newtonsoft.Json;

namespace FoxIDs.Models.Logic.Health
{
    public record class HealthCheckResult
    {
        [JsonProperty(PropertyName = "component")]
        public required string Component { get; init; }

        [JsonProperty(PropertyName = "status")]
        public HealthCheckStatus Status { get; init; }

        [JsonProperty(PropertyName = "message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; init; }

        public static HealthCheckResult Healthy(string component, string message = null) =>
            new()
            {
                Component = component,
                Status = HealthCheckStatus.Healthy,
                Message = message
            };

        public static HealthCheckResult Unhealthy(string component, string message = null) =>
            new()
            {
                Component = component,
                Status = HealthCheckStatus.Unhealthy,
                Message = message
            };

        public static HealthCheckResult Skipped(string component, string message = null) =>
            new()
            {
                Component = component,
                Status = HealthCheckStatus.Skipped,
                Message = message
            };
    }
}
