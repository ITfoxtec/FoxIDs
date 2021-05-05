using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ScopedStreamApplicationInsightsSettings
    {
        [Required]
        [MaxLength(Constants.Models.Track.Logging.ApplicationInsightsKeyLength)]
        [JsonProperty(PropertyName = "instrumentation_key")]
        public string InstrumentationKey { get; set; }
    }
}
