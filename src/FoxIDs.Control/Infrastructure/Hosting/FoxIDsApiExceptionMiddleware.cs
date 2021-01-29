using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.Models;
using AutoMapper;
using System;

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
            catch (HttpStatusException mhse)
            {
                scopedLogger.Error(mhse);
                await HandleHttpStatusExceptionAsync(httpContext, mhse);
            }
            catch (AutoMapperMappingException amme)
            {
                scopedLogger.Error(amme);
                var httpStatusException = FindInnerHttpStatusException(amme);
                if (httpStatusException != null)
                {
                    await HandleHttpStatusExceptionAsync(httpContext, httpStatusException);
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                scopedLogger.Error(ex);
                throw;
            }
        }

        private HttpStatusException FindInnerHttpStatusException(Exception ex)
        {
            if (ex.InnerException == null)
            {
                return null;
            }
            else if (ex.InnerException is HttpStatusException httpStatusException)
            {
                return httpStatusException;
            }
            else
            {
                return FindInnerHttpStatusException(ex.InnerException);
            }
        }

        private static async Task HandleHttpStatusExceptionAsync(HttpContext httpContext, HttpStatusException hse)
        {
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)hse.StatusCode;
            await httpContext.Response.WriteAsync(hse.Message);
        }
    }
}
