using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ScopedStreamApplicationInsightsSettings
    {
        [Required]
        [MaxLength(Constants.Models.Logging.ApplicationInsightsConnectionStringLength)]
        [RegularExpression(Constants.Models.Logging.ApplicationInsightsConnectionStringRegExPattern)]
        [JsonProperty(PropertyName = "connection_string")]
        public string ConnectionString { get; set; }
    }
}
