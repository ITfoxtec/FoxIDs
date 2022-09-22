using FoxIDs.Logic;
using FoxIDs.Repository;
using ITfoxtec.Identity.Discovery;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using StackExchange.Redis;
using System;
using System.Net.Http;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSharedLogic(this IServiceCollection services)
        {
            services.AddTransient<PlanUsageLogic>();

            services.AddTransient<ExternalSecretLogic>();
            services.AddTransient<ExternalKeyLogic>();

            services.AddTransient<ClaimTransformValidationLogic>();

            services.AddTransient<TenantCacheLogic>();
            services.AddTransient<TrackCacheLogic>();
            services.AddTransient<DownPartyCacheLogic>();
            services.AddTransient<UpPartyCacheLogic>();

            return services;
        }

        public static IServiceCollection AddSharedRepository(this IServiceCollection services)
        {            
            services.AddSingleton<IRepositoryClient, RepositoryClient>();
            services.AddSingleton<IRepositoryBulkClient, RepositoryBulkClient>();
            services.AddSingleton<IMasterRepository, MasterRepository>();
            services.AddSingleton<ITenantRepository, TenantRepository>();

            return services;
        }

        public static (IServiceCollection, IConnectionMultiplexer) AddSharedInfrastructure(this IServiceCollection services, Models.Config.Settings settings)
        {
            IdentityModelEventSource.ShowPII = true;

            services.AddHsts(options =>
            {
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            services.AddCors();

            services.AddSingleton<TelemetryLogger>();
            services.AddSingleton<TelemetryScopedStreamLogger>();
            services.AddScoped<TelemetryScopedLogger>();
            services.AddScoped<TelemetryScopedProperties>();

            services.AddHttpContextAccessor();
            services.AddHttpClient(nameof(HttpClient), options => 
            { 
                options.MaxResponseContentBufferSize = 500000; // 500kB 
                options.Timeout = TimeSpan.FromSeconds(30);
            });

            var connectionMultiplexer = ConnectionMultiplexer.Connect(settings.RedisCache.ConnectionString);
            services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);

            services.AddSingleton<OidcDiscoveryHandlerService>();
            services.AddHostedService<OidcDiscoveryBackgroundService>();

            return (services, connectionMultiplexer);
        }
    }
}
