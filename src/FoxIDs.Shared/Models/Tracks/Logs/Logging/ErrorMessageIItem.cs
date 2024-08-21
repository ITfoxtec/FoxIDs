using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models
{
    public class ErrorMessageItem
    {
        [JsonProperty(PropertyName = "m")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "s")]
        public IEnumerable<string> StackTrace { get; set; }
    }
}
