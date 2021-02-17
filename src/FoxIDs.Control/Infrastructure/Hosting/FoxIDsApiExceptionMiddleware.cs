using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using System;
using System.Net;

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
            catch (Exception ex)
            {
                scopedLogger.Error(ex);
                await HandleHttpStatusExceptionAsync(httpContext, ex.Message, HttpStatusCode.BadRequest);
            }
        }

        private static async Task HandleHttpStatusExceptionAsync(HttpContext httpContext, string message, HttpStatusCode statusCode)
        {
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)statusCode;
            await httpContext.Response.WriteAsync(message);
        }
    }
}
