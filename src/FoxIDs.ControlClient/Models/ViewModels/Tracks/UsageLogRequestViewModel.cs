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
        public List<string> IncludeTypes { get; set; } = new List<string> { UsageLogIncludeTypes.Tenants, UsageLogIncludeTypes.Tracks, UsageLogIncludeTypes.Users, UsageLogIncludeTypes.Logins, UsageLogIncludeTypes.TokenRequests };

        [Display(Name = "Select one tenant by full tenant name")]
        public string TenantName { get; set; } 

        [Display(Name = "Select one environment by full environment name")]
        public string TrackName { get; set; }       
    }
}
