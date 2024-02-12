using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace FoxIDs.Controllers.Client
{
    public class WController : Controller
    {
        private static string indexFile;
        private readonly IWebHostEnvironment currentEnvironment;

        public WController(IWebHostEnvironment env)
        {
            currentEnvironment = env;
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
                indexFile = indexFile.Replace("{version}", BuildInfo.CompilationTimestampUtc.ToString("yyyyMMddHHmmss"));
                indexFile = indexFile.Replace("{min}", currentEnvironment.IsDevelopment() ? string.Empty : ".min");
            }
            return Content(indexFile, "text/html");
        }
    }
}
