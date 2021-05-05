using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{ 
    public class LogStreamApplicationInsightsSettings
    {
        [Required]
        [MaxLength(Constants.Models.Track.Logging.ApplicationInsightsKeyLength)]
        [Display(Name = "Instrumentation key")]
        public string InstrumentationKey { get; set; }
    }
}
