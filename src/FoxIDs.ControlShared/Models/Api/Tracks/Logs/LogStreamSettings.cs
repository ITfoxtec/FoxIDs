using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Log stream settings in track.
    /// </summary>
    public class LogStreamSettings : LogSettings, IValidatableObject
    {
        [Required]
        public LogStreamTypes Type { get; set; }

        [Display(Name = "Log warnings")]
        public bool LogWarning { get; set; }

        [Display(Name = "Log errors")]
        public bool LogError { get; set; }

        [Display(Name = "Log critical error")]
        public bool LogCriticalError { get; set; }

        [Display(Name = "Log events to see events about the login and logout sequences")]
        public bool LogEvent { get; set; }

        [ValidateComplexType]
        public LogStreamApplicationInsightsSettings ApplicationInsightsSettings { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Type == LogStreamTypes.ApplicationInsights && ApplicationInsightsSettings == null)
            {
                results.Add(new ValidationResult($"The field {nameof(ApplicationInsightsSettings)} is required for log stream type '{Type}'.", new[] { nameof(ApplicationInsightsSettings) }));
            }
            return results;
        }
    }
}
