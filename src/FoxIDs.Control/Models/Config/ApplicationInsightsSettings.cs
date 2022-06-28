using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class ApplicationInsightsSettings
    {
        /// <summary>
        /// ApplicationInsights Workspace ID.
        /// </summary>
        [Required]
        public string WorkspaceId { get; set; }
    }
}
