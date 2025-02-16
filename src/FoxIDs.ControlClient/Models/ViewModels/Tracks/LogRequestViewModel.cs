using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class LogRequestViewModel : IValidatableObject
    {
        [Required]
        [Display(Name = "From")]
        public string FromTime { get; set; } = DefaultFromTime.ToString();

        [Display(Name = "Interval")]
        public LogTimeIntervals TimeInterval { get; set; } = LogTimeIntervals.FifteenMinutes;

        [Display(Name = "Select tenant by full tenant name")]
        public string TenantName { get; set; }

        [Display(Name = "Select environment by full environment name")]
        public string TrackName { get; set; }

        [Display(Name = "Search")]
        public string Filter { get; set; }

        [Display(Name = "Log types")]
        public List<string> QueryTypes { get; set; }

        public static DateTimeOffset DefaultFromTime => DateTimeOffset.Now.AddMinutes(-5);

        public bool DisableBothEventAndTrace { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (DisableBothEventAndTrace)
            {
                if (QueryTypes.Contains(LogQueryTypes.Traces) && QueryTypes.Contains(LogQueryTypes.Events))
                {
                    results.Add(new ValidationResult($"Traces and events cannot be selected at the same time.", [nameof(QueryTypes)]));
                }
            }

            return results;
        }
    }
}
