using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Diagnostics;
using System;
using FoxIDs.Repository;
using FoxIDs.Models;

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

        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            return GetProcessedIndexFile(GetTechnicalError(exceptionHandlerPathFeature?.Error));
        }

        private string GetTechnicalError(Exception exception)
        {
            if (exception != null)
            {
                var dataException = FindException<FoxIDsDataException>(exception);
                if (dataException != null && dataException.StatusCode == DataStatusCode.NotFound)
                {
                    return $"Unknown tenant{GetTenantName(dataException)}.";
                }
                else
                {
                    return exception.Message;
                }
            }

            return "Unknown error";
        }

        private string GetTenantName(FoxIDsDataException dataException)
        {
            var eSplit = dataException.Message.Split(':');
            if (eSplit.Length > 1)
            {
                eSplit = eSplit[1].Split('\'');
                return $" '{eSplit[0]}'";
            }
            return string.Empty;
        }

        private IActionResult GetProcessedIndexFile(string technicalError = null)
        {
            if (indexFile == null)
            {
                var file = currentEnvironment.WebRootFileProvider.GetFileInfo("index.html");
                indexFile = System.IO.File.ReadAllText(file.PhysicalPath);
                indexFile = indexFile.Replace("{version}", GetBuildDate());
            }
            return Content(AddErrorInfo(indexFile, technicalError), "text/HTML");
        }

        private string AddErrorInfo(string indexFile, string technicalError)
        {
            if (technicalError.IsNullOrEmpty())
            {
                return indexFile.Replace("{error}", string.Empty);
            }
            else
            {
                var errorInfo = new ErrorInfo
                {
                    CreateTime = DateTimeOffset.Now.ToUnixTimeSeconds(),
                    RequestId = HttpContext.TraceIdentifier,
                    TechnicalError = technicalError
                };
                return indexFile.Replace("{error}", errorInfo.ToJson());
            }
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

        private T FindException<T>(Exception exception) where T : Exception
        {
            if (exception is T)
            {
                return exception as T;
            }
            else if (exception.InnerException != null)
            {
                return FindException<T>(exception.InnerException);
            }
            else
            {
                return null;
            }
        }
    }
}
