using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{ 
    /// <summary>
    /// Settings for streaming logs to Application Insights.
    /// </summary>
    public class LogStreamApplicationInsightsSettings
    {
        /// <summary>
        /// Application Insights connection string.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Logging.ApplicationInsightsConnectionStringLength)]
        [RegularExpression(Constants.Models.Logging.ApplicationInsightsConnectionStringRegExPattern)]
        [Display(Name = "Connection string")]
        public string ConnectionString { get; set; }
    }
}
