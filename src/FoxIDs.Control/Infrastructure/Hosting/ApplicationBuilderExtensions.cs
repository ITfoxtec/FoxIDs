using FoxIDs.Infrastructure.Filters;
using FoxIDs.Models.Config;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

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

                    var response = await httpClient.GetAsync($"{context.Request.Scheme}://{context.Request.Host.ToUriComponent()}/api/swagger/{Constants.ControlApi.Version}/swagger.json", HttpCompletionOption.ResponseHeadersRead);
                    await using var responseStream = await response.Content.ReadAsStreamAsync();

                    var json = JsonNode.Parse(responseStream) as JsonObject;
                    if (json?["info"] is JsonObject info)
                    {
                        info["version"] = "v1";
                    }

                    if (json?["paths"] is JsonObject paths)
                    {
                        var newPaths = new JsonObject();
                        foreach (var path in paths.ToList())
                        {
                            var key = path.Key.Replace("{tenant_name}/{track_name}", "[tenant_name]/[track_name]");

                            if (path.Value is JsonObject pathObj)
                            {
                                var pathObjClone = pathObj.DeepClone() as JsonObject ?? new JsonObject();
                                foreach (var op in pathObj.Where(o => o.Value is JsonObject).ToList())
                                {
                                    if (op.Value is JsonObject opObj && opObj["parameters"] is JsonArray parameters)
                                    {
                                        var filtered = parameters
                                            .OfType<JsonObject>()
                                            .Where(p => !string.Equals(p["name"]?.GetValue<string>(), "tenant_name", StringComparison.OrdinalIgnoreCase)
                                                        && !string.Equals(p["name"]?.GetValue<string>(), "track_name", StringComparison.OrdinalIgnoreCase))
                                            .ToList();

                                        var newArray = new JsonArray();
                                        foreach (var p in filtered)
                                        {
                                            newArray.Add(p.DeepClone());
                                        }
                                        opObj["parameters"] = newArray;
                                    }
                                }

                                newPaths[key] = pathObjClone;
                            }
                            else
                            {
                                newPaths[key] = path.Value?.DeepClone();
                            }
                        }

                        json["paths"] = newPaths;
                    }

                    var stringBuilder = new StringBuilder();
                    using (var stringWriter = new StringWriter(stringBuilder))
                    {
                        stringWriter.Write(json?.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) ?? string.Empty);
                    }

                    context.Response.StatusCode = (int)response.StatusCode;
                    context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "text/html";
                    ApplySwaggerSecurityHeaders(context);

                    await context.Response.WriteAsync(stringBuilder.ToString());
                }
                else
                {
                    await next();
                }
            });

            builder.Use(async (context, next) =>
            {
                var applySecurityHeaders = context.Request.Path.StartsWithSegments("/api/swagger", StringComparison.OrdinalIgnoreCase);

                if (applySecurityHeaders)
                {
                    context.Response.OnStarting(() =>
                    {
                        ApplySwaggerSecurityHeaders(context);
                        return Task.CompletedTask;
                    });
                }

                await next();
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
                c.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
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
        
        private static void ApplySwaggerSecurityHeaders(HttpContext context)
        {
            var settings = context.RequestServices.GetRequiredService<FoxIDsControlSettings>();
            var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
            var securityHeaders = new FoxIDsControlHttpSecurityHeadersAttribute.FoxIDsControlHttpSecurityHeadersActionAttribute(settings, environment);

            var contentType = context.Response.ContentType;
            var isHtml = !string.IsNullOrEmpty(contentType) && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
            securityHeaders.ApplyFromMiddleware(context, isHtml);
        }
    }
}
