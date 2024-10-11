using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class UsageSettings
    {
        /// <summary>
        /// The usage calculator background service wait period in seconds.
        /// </summary>
        [Required]
        public int BackgroundServiceWaitPeriod { get; set; } = 7200; // 2 hours
    }
}
