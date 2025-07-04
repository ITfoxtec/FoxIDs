using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class ApplicationBuilderExtensions
    {
        public static void UseApiSwagger(this IApplicationBuilder builder)
        {
            // Support Swagger V1
            builder.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/api/swagger/v1/swagger.json", StringComparison.OrdinalIgnoreCase))
                {
                    var httpClientFactory = context.RequestServices.GetService<IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient();

                    var response = await httpClient.GetAsync($"{context.Request.Scheme}://{context.Request.Host.ToUriComponent()}/api/swagger/{Constants.ControlApi.Version}/swagger.json");
                    var result = await response.Content.ReadAsStringAsync();

                    // Replace to support Swagger V1
                    result = result.Replace("\"version\": \"v2\"", "\"version\": \"v1\"");
                    result = result.Replace("{tenant_name}/{track_name}", "[tenant_name]/[track_name]");

                    context.Response.StatusCode = (int)response.StatusCode;
                    context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "text/html";

                    await context.Response.WriteAsync(result);
                }
                else
                {
                    await next();
                }
            });

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
