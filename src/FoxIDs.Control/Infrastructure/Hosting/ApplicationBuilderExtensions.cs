using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;
using System;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class ApplicationBuilderExtensions
    {
        public static void UseApiSwagger(this IApplicationBuilder builder)
        {
            builder.Use((context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/api/swagger", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                }
                return next.Invoke();
            });
            builder.UseSwagger(c =>
            {
                c.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
                c.PreSerializeFilters.Add((openApiDocument, httpRequest) =>
                {
                    openApiDocument.Servers = new List<OpenApiServer> { new OpenApiServer { Url = UrlCombine.Combine(httpRequest.HttpContext.GetHost(addTrailingSlash: false), Constants.Routes.ApiPath) } };
                });
                c.RouteTemplate = "api/swagger/{documentname}/swagger.json";
            });

            builder.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/api/swagger/{Constants.ControlApi.Version}/swagger.json", "FoxIDs Control API");
                c.RoutePrefix = "api/swagger";
            });
        }

        public static IApplicationBuilder UseProxyMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ProxyHeadersMiddleware>();
        }

        public static IApplicationBuilder UseClientRouteBindingMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<FoxIDsClientRouteBindingMiddleware>();
        }

        public static IApplicationBuilder UseApiRouteBindingMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<FoxIDsApiRouteBindingMiddleware>();
        } 
        public static IApplicationBuilder UseApiExceptionMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<FoxIDsApiExceptionMiddleware>();
        }
    }
}
