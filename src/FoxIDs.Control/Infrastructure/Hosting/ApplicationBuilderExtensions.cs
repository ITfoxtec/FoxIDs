using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class ApplicationBuilderExtensions
    {
        public static void UseApiSwagger(this IApplicationBuilder builder)
        {
            builder.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((openApiDocument, httpRequest) =>
                {
                    openApiDocument.Servers = new List<OpenApiServer> { new OpenApiServer { Url = UrlCombine.Combine(httpRequest.HttpContext.GetHost(addTrailingSlash: false), Constants.Routes.ApiPath) } };
                });
                c.RouteTemplate = "api/swagger/{documentname}/swagger.json";
            });
#if DEBUG
            builder.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/api/swagger/{Constants.ControlApi.Version}/swagger.json", "FoxIDs Control API");
            });
#endif
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
