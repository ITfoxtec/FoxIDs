using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using System;
using System.Net;
using Microsoft.AspNetCore.Routing;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsApiExceptionMiddleware 
    {
        private readonly RequestDelegate next;

        public FoxIDsApiExceptionMiddleware(RequestDelegate next)
        {
            this.next = next;
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
                scopedLogger.Error(amme);
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
                    scopedLogger.Error(ex);
                    await HandleHttpStatusExceptionAsync(httpContext, ex.GetAllMessagesJoined(), HttpStatusCode.BadRequest);
                }
            }
        }

        private async Task HandleRouteCreationException(HttpContext httpContext, TelemetryScopedLogger scopedLogger, RouteCreationException rce)
        {
            scopedLogger.Error(rce);
            await HandleHttpStatusExceptionAsync(httpContext, rce.Message, HttpStatusCode.BadRequest);
        }

        private static async Task HandleHttpStatusExceptionAsync(HttpContext httpContext, string message, HttpStatusCode statusCode)
        {
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)statusCode;
            await httpContext.Response.WriteAsync(message);
        }
    }
}
