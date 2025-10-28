using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models
{
    public class LogExceptionDetail
    {
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "parsedStack")]
        public List<LogExceptionPassedStack> ParsedStack { get; set; }
    }
}
