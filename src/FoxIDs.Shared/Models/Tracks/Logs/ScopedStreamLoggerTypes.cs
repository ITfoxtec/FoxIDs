using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum ScopedStreamLoggerTypes
    {
        [EnumMember(Value = "application_insights")]
        ApplicationInsights = 0,
    }
}
