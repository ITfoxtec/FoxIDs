using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ScopedStreamApplicationInsightsSettings
    {
        [Required]
        string connectionString;
        [MaxLength(Constants.Models.Track.Logging.ApplicationInsightsConnectionStringLength)]
        [JsonProperty(PropertyName = "connection_string")]
        public string ConnectionString 
        {
            get 
            {
                if (connectionString.IsNullOrWhiteSpace() && !InstrumentationKey.IsNullOrWhiteSpace())
                {
                    return $"InstrumentationKey={InstrumentationKey}";
                }
                return connectionString;
            }
            set
            {
                connectionString = value;
            }
        }

        [Obsolete("ApplicationInsights InstrumentationKey is being deprecated. See https://github.com/microsoft/ApplicationInsights-dotnet/issues/2560 for more details.")]
        [MaxLength(Constants.Models.Track.Logging.ApplicationInsightsKeyLength)]
        [JsonProperty(PropertyName = "instrumentation_key")]
        public string InstrumentationKey { get; set; }
    }
}
