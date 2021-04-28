using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using System;
using System.Net;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsApiExceptionMiddleware 
    {
        private readonly RequestDelegate next;
        private readonly IWebHostEnvironment environment;

        public FoxIDsApiExceptionMiddleware(RequestDelegate next, IWebHostEnvironment environment)
        {
            this.next = next;
            this.environment = environment;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var scopedLogger = httpContext.RequestServices.GetService<TelemetryScopedLogger>();
            try
            {
                await next(httpContext);
            }
            catch (AutoMapperMappingException amme)
            {
                LogError(scopedLogger, amme);
                await HandleHttpStatusExceptionAsync(httpContext, amme.Message, HttpStatusCode.BadRequest);
            }
            catch (RouteCreationException rce)
            {
                await HandleRouteCreationException(httpContext, scopedLogger, rce);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is RouteCreationException rce)
                {
                    await HandleRouteCreationException(httpContext, scopedLogger, rce);
                }
                else
                {
                    LogError(scopedLogger, ex);
                    await HandleHttpStatusExceptionAsync(httpContext, ex.GetAllMessagesJoined(), HttpStatusCode.BadRequest);
                }
            }
        }

        private async Task HandleRouteCreationException(HttpContext httpContext, TelemetryScopedLogger scopedLogger, RouteCreationException rce)
        {
            LogError(scopedLogger, rce);
            await HandleHttpStatusExceptionAsync(httpContext, rce.Message, HttpStatusCode.BadRequest);
        }

        private static async Task HandleHttpStatusExceptionAsync(HttpContext httpContext, string message, HttpStatusCode statusCode)
        {
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)statusCode;
            await httpContext.Response.WriteAsync(message);
        }

        private void LogError(TelemetryScopedLogger scopedLogger, Exception ex)
        {
            scopedLogger.Error(ex);
            if (environment.IsDevelopment())
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
