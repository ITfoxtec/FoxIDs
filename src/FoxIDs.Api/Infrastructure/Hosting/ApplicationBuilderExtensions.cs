using Microsoft.AspNetCore.Builder;
using System.Collections.Generic;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class ApplicationBuilderExtensions
    {
        public static void UseApiSwagger(this IApplicationBuilder builder)
        {
            builder.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                {
                    swaggerDoc.Host = httpReq.Host.Value;
                    swaggerDoc.Schemes = new List<string>() { httpReq.Scheme };
                });
            });
#if DEBUG
            builder.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "FoxIDs API");
            });
#endif
        }

    }
}
