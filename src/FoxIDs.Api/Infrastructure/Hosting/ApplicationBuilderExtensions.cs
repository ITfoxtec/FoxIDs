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
                    openApiDocument.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"{httpRequest.Scheme}://{httpRequest.Host.Value}" } };
                });
            });
#if DEBUG
            builder.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/{Constants.Api.Version}/swagger.json", "FoxIDs API");
            });
#endif
        }

        public static IApplicationBuilder UseRouteBindingMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<FoxIDsApiRouteBindingMiddleware>();
        }
    }
}
