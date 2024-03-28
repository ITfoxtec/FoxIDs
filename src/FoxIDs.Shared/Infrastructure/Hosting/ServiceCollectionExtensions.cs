using FoxIDs.Logic;
using FoxIDs.Logic.Caches.Providers;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity.Discovery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using System;
using System.Net.Http;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSharedLogic(this IServiceCollection services, Settings settings)
        {
            services.AddTransient<PlanUsageLogic>();

            if (settings.Options.KeyStorage == KeyStorageOptions.KeyVault)
            {
                services.AddTransient<ExternalSecretLogic>();
                services.AddTransient<ExternalKeyLogic>();
            }

            services.AddTransient<ClaimTransformValidationLogic>();

            switch (settings.Options.Cache)
            {
                case CacheOptions.Memory:
                    services.AddSingleton<IMemoryCache, MemoryCache>();
                    services.AddTransient<ICacheProvider, MemoryCacheProvider>();
                    break;
                case CacheOptions.File:
                    services.AddTransient<ICacheProvider, FileCacheProvider>();
                    break;
                case CacheOptions.Redis:
                    services.AddTransient<ICacheProvider, RedisCacheProvider>();
                    break;
                default:
                    throw new NotSupportedException($"{nameof(settings.Options.Cache)} Cache option '{settings.Options.Cache}' not supported.");
            }

            switch (settings.Options.DataCache)
            {
                case DataCacheOptions.None:
                    services.AddTransient<IDataCacheProvider, InactiveCacheProvider>();
                    break;
                case DataCacheOptions.Default:
                    services.AddTransient<IDataCacheProvider, RedisCacheProvider>();
                    break;
                default:
                    throw new NotSupportedException($"{nameof(settings.Options.DataCache)} option '{settings.Options.DataCache}' not supported.");
            }

            services.AddTransient<PlanCacheLogic>();
            services.AddTransient<TenantCacheLogic>();
            services.AddTransient<TrackCacheLogic>();
            services.AddTransient<DownPartyCacheLogic>();
            services.AddTransient<UpPartyCacheLogic>();

            return services;
        }

        public static IServiceCollection AddSharedRepository(this IServiceCollection services, Settings settings)
        {
            if (settings.Options.DataStorage == DataStorageOptions.File || settings.Options.Cache == CacheOptions.File)
            {
                services.AddSingleton<FileDataRepository>();
            }

            switch (settings.Options.DataStorage)
            {
                case DataStorageOptions.Memory:
                    services.AddSingleton<MemoryDataRepository>();
                    services.AddSingleton<IMasterDataRepository, MemoryMasterDataRepository>();
                    services.AddSingleton<ITenantDataRepository, MemoryTenantDataRepository>();
                    break;
                case DataStorageOptions.File:
                    services.AddSingleton<IMasterDataRepository, FileMasterDataRepository>();
                    services.AddSingleton<ITenantDataRepository, FileTenantDataRepository>();
                    break;
                case DataStorageOptions.CosmosDb:
                    services.AddSingleton<ICosmosDbDataRepositoryClient, CosmosDbDataRepositoryClient>();
                    services.AddSingleton<ICosmosDbDataRepositoryBulkClient, CosmosDbDataRepositoryBulkClient>();
                    services.AddSingleton<IMasterDataRepository, CosmosDbMasterDataRepository>();
                    services.AddSingleton<ITenantDataRepository, CosmosDbTenantDataRepository>();
                    break;
                case DataStorageOptions.MongoDb:
                    throw new NotImplementedException();
                    //break;
                default:
                    throw new NotSupportedException($"{nameof(settings.Options.DataStorage)} option '{settings.Options.DataStorage}' not supported.");
            }

            return services;
        }

        public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, Settings settings, IWebHostEnvironment env)
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
            var httpClientBuilder = services.AddHttpClient(Options.DefaultName, options => 
            { 
                options.MaxResponseContentBufferSize = 500000; // 500kB 
                options.Timeout = TimeSpan.FromSeconds(30);
            });
            if (env.IsDevelopment()) {
                httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
                    new HttpClientHandler { ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true }
                );
            }

            services.AddSingleton<OidcDiscoveryHandlerService>();
            services.AddHostedService<OidcDiscoveryBackgroundService>();

            return services;
        }
    }
}
