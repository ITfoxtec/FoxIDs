using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Reflection;
using System;

namespace FoxIDs.Controllers.Client
{
    public class WController : Controller
    {
        private static string indexFile;
        private readonly IWebHostEnvironment currentEnvironment;

        public WController(IWebHostEnvironment environment)
        {
            currentEnvironment = environment;
        }

        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index()
        {
            return GetProcessedIndexFile();
        }

        private IActionResult GetProcessedIndexFile()
        {
            if (indexFile == null)
            {
                var file = currentEnvironment.WebRootFileProvider.GetFileInfo("index.html");
                indexFile = System.IO.File.ReadAllText(file.PhysicalPath);
                indexFile = indexFile.Replace("{version}", GetBuildDate().ToString("yyyyMMddHHmmss"));
                indexFile = indexFile.Replace("{min}", currentEnvironment.IsDevelopment() ? string.Empty : ".min");
            }
            return Content(indexFile, "text/html");
        }

        private static DateTime GetBuildDate()
        {
            const string BuildVersionMetadataPrefix = "+build";

            var attribute = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (!string.IsNullOrWhiteSpace(attribute?.InformationalVersion))
            {
                var index = attribute.InformationalVersion.IndexOf(BuildVersionMetadataPrefix);
                if (index > 0)
                {
                    var dateTimeValue = attribute.InformationalVersion.Substring(index + BuildVersionMetadataPrefix.Length);
                    if (DateTime.TryParseExact(dateTimeValue, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                    {
                        return result;
                    }
                }
            }
            return default;
        }
    }
}
