using System.Collections.Generic;
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

        /// <summary>
        /// Optionally, ApplicationInsights Workspace ID used in plans.
        /// </summary>
        public List<string> PlanWorkspaceIds { get; set; }
    }
}
