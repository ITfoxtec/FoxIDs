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

        [Display(Name = "Search")]
        public string Filter { get; set; }

        [Display(Name = "Log types")]
        public List<string> QueryTypes { get; set; } = new List<string> { LogQueryTypes.Exceptions, LogQueryTypes.Events };

        public static DateTimeOffset DefaultFromTime => DateTimeOffset.Now.AddMinutes(-5);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (QueryTypes.Contains(LogQueryTypes.Traces) && QueryTypes.Contains(LogQueryTypes.Events))
            {
                results.Add(new ValidationResult($"Traces and events cannot be selected at the same time.", new[] { nameof(QueryTypes) }));
            }
            return results;
        }
    }
}
