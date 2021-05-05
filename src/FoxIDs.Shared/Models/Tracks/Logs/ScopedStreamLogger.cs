using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ScopedStreamLogger : ScopedLogger, IValidatableObject
    {
        [Required]
        [JsonProperty(PropertyName = "type")]
        public ScopedStreamLoggerTypes Type { get; set; }

        [JsonProperty(PropertyName = "log_warning")]
        public bool LogWarning { get; set; }

        [JsonProperty(PropertyName = "log_error")]
        public bool LogError { get; set; }

        [JsonProperty(PropertyName = "log_critical_error")]
        public bool LogCriticalError { get; set; }

        [JsonProperty(PropertyName = "log_event")]
        public bool LogEvent { get; set; }

        [ValidateComplexType]
        [JsonProperty(PropertyName = "application_insights_settings")]
        public ScopedStreamApplicationInsightsSettings ApplicationInsightsSettings { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Type == ScopedStreamLoggerTypes.ApplicationInsights && ApplicationInsightsSettings == null)
            {
                results.Add(new ValidationResult($"The field {nameof(ApplicationInsightsSettings)} is required for scoped stream logger type '{Type}'.", new[] { nameof(ApplicationInsightsSettings) }));
            }
            return results;
        }
    }
}
