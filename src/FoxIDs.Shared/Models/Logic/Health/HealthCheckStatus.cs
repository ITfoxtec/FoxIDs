using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FoxIDs.Models.Logic.Health
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum HealthCheckStatus
    {
        Healthy,
        Unhealthy,
        Skipped,
        Invalid
    }
}
