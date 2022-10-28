using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{ 
    public class LogStreamApplicationInsightsSettings
    {
        [Required]
        string connectionString;
        [MaxLength(Constants.Models.Track.Logging.ApplicationInsightsConnectionStringLength)]
        [Display(Name = "Connection string")]
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
        [Display(Name = "Instrumentation key")]
        public string InstrumentationKey { get; set; }
    }
}
