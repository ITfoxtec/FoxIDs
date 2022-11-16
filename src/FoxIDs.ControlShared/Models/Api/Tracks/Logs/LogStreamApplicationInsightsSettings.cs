using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{ 
    public class LogStreamApplicationInsightsSettings
    {
        [Required]
        [MaxLength(Constants.Models.Logging.ApplicationInsightsConnectionStringLength)]
        [RegularExpression(Constants.Models.Logging.ApplicationInsightsConnectionStringRegExPattern)]
        [Display(Name = "Connection string")]
        public string ConnectionString { get; set; }
    }
}
