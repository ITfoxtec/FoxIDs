using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class AuditLogRequestViewModel
    {
        [Required]
        [Display(Name = "From")]
        public string FromTime { get; set; } = LogRequestViewModel.DefaultFromTime.ToString();

        [Display(Name = "Interval")]
        public LogTimeIntervals TimeInterval { get; set; } = LogTimeIntervals.FifteenMinutes;

        [Display(Name = "Select tenant by full tenant name")]
        public string TenantName { get; set; }

        [Display(Name = "Select environment by full environment name")]
        public string TrackName { get; set; }

        [Display(Name = "Search")]
        public string Filter { get; set; }
    }
}
