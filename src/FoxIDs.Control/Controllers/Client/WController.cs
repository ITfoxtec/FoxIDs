using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Infrastructure.Hosting;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Reflection;

namespace FoxIDs.Controllers.Client
{
   // [HttpSecurityHeaders]
    public class WController : Controller
    {
        private static string indexFile;
        private readonly TelemetryScopedLogger logger;
        private readonly IWebHostEnvironment currentEnvironment;
        private readonly FoxIDsControlSettings settings;

        public WController(TelemetryScopedLogger logger, IWebHostEnvironment environment, FoxIDsControlSettings settings)
        {
            this.logger = logger;
            currentEnvironment = environment;
            this.settings = settings;
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
            var exception = exceptionHandlerPathFeature?.Error;

            var routeException = FindException<RouteException>(exception);
            if (routeException != null)
            {
                LogExceptionAsWarning(routeException);
            }
            else
            {
                LogExceptionAsError(exception);
            }

            return GetProcessedIndexFile(GetTechnicalError(exception));
        }

        private void LogExceptionAsWarning(Exception exception)
        {
            if (exception == null)
            {
                LogExceptionAsError(exception);
            }
            else
            {
                logger.Warning(exception);
            }
        }

        private void LogExceptionAsError(Exception exception)
        {
            if (exception == null)
            {
                exception = new Exception("Unknown error");
            }
            logger.Error(exception);
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
                if(settings.Payment?.EnablePayment == true && settings.Usage?.EnableInvoice == true)
                {
                    indexFile = indexFile.Replace("{payment_script}", "<script src=\"https://js.mollie.com/v1/mollie.js\"></script>");
                }
                else
                {
                    indexFile = indexFile.Replace("{payment_script}", string.Empty);
                }
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
