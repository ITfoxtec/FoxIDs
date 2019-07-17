using FoxIDs.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSharedLogic(this IServiceCollection services)
        {                       

            return services;
        }

        public static IServiceCollection AddSharedRepository(this IServiceCollection services)
        {            
            services.AddSingleton<IRepositoryClient, RepositoryClient>();
            services.AddSingleton<IMasterRepository, MasterRepository>();
            services.AddSingleton<ITenantRepository, TenantRepository>();

            return services;
        }

        public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<TelemetryLogger>();
            services.AddSingleton<TenantTrackLogger>();
            services.AddScoped<TelemetryScopedLogger>();

            return services;
        }
    }
}
