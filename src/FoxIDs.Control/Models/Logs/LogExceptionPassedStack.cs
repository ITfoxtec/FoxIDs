using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;

namespace FoxIDs.Models
{
    public class LogExceptionPassedStack
    {
        [JsonProperty(PropertyName = "method")]
        public string Method { get; set; }

        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; set; }

        [JsonProperty(PropertyName = "line")]
        public string Line { get; set; }

        public override string ToString()
        {
            var fileName = FileName;
            if (!fileName.IsNullOrEmpty())
            {
                var fileNameIndex = fileName.IndexOf(@"\FoxIDs\src\", StringComparison.OrdinalIgnoreCase);
                if (fileNameIndex > -1)
                {
                    fileName = $".{fileName.Substring(fileNameIndex)}";
                }

                return $"{Method}, {fileName} line {Line}";
            }
            else
            {
                return Method;
            }
        }
    }
}
