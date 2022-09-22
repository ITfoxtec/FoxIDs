using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class UsageLogRequestViewModel
    {
        [Required]
        [Display(Name = "Time scope")]
        public UsageLogTimeScopes TimeScope { get; set; } = UsageLogTimeScopes.ThisMonth;

        [Required]
        [Display(Name = "Summarize level")]
        public UsageLogSummarizeLevels SummarizeLevel { get; set; } = UsageLogSummarizeLevels.Month;

        [Display(Name = "Usage types")]
        public List<string> IncludeTypes { get; set; } = new List<string> { UsageLogIncludeTypes.Logins, UsageLogIncludeTypes.TokenRequests };

        [Display(Name = "Select one track by full track name")]
        public string TrackName { get; set; }       
    }
}
