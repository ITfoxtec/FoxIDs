using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Reflection;
using System;
using ITfoxtec.Identity;

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
                indexFile = indexFile.Replace("{version}", GetBuildDate());
            }
            return Content(indexFile, "text/html");
        }

        private static string GetBuildDate()
        {
            var attribute = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (!string.IsNullOrWhiteSpace(attribute?.InformationalVersion))
            {
                var versionSplit = attribute.InformationalVersion.Split('+');
                if (versionSplit?.Length >= 1)
                {
                    var version = versionSplit[0];
                    if (!version.IsNullOrEmpty())
                    {
                        return version;
                    }
                }
            }
            return default;
        }
    }
}
