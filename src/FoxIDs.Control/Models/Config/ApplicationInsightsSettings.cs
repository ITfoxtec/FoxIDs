using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class ApplicationInsightsSettings
    {
        /// <summary>
        /// ApplicationInsights app ID.
        /// </summary>
        [Required]
        public string AppId { get; set; }
    }
}
