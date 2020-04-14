using Microsoft.AspNetCore.Builder;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseRouteBindingMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<FoxIDsRouteBindingMiddleware>();
        }
    }
}
