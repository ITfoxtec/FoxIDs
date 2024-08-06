using FoxIDs.Infrastructure.Logging;
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
using MongoDB.Driver;
using StackExchange.Redis;
using Microsoft.AspNetCore.DataProtection;
using System;
using System.Net.Http;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSharedLogic(this IServiceCollection services, Settings settings)
        {
            services.AddTransient<PlanUsageLogic>();
            services.AddTransient<ContainedKeyLogic>();

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
                case CacheOptions.MongoDb:
                    services.AddTransient<ICacheProvider, MongoDbCacheProvider>();
                    break;
                case CacheOptions.PostgreSql:
                    services.AddPgKeyValueDB(settings.PostgreSql.ConnectionString, a => a.TableName = settings.PostgreSql.TableName, ServiceLifetime.Singleton, Constants.Models.DataType.Cache);
                    services.AddTransient<ICacheProvider, PostgreSqlCacheProvider>();
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

            if (settings.Options.DataStorage == DataStorageOptions.MongoDb || settings.Options.Cache == CacheOptions.MongoDb)
            {
                services.AddSingleton<IMongoClient>(s => new MongoClient(settings.MongoDb.ConnectionString));
                services.AddSingleton<MongoDbRepositoryClient>();
            }

            switch (settings.Options.DataStorage)
            {
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
                    services.AddSingleton<IMasterDataRepository, MongoDbMasterDataRepository>();
                    services.AddSingleton<ITenantDataRepository, MongoDbTenantDataRepository>();
                    break;
                case DataStorageOptions.PostgreSql:
                    services.AddPgKeyValueDB(settings.PostgreSql.ConnectionString, a => a.TableName = settings.PostgreSql.TableName, ServiceLifetime.Singleton, Constants.Models.DataType.Master);
                    services.AddSingleton<IMasterDataRepository, PgMasterDataRepository>();
                    services.AddPgKeyValueDB(settings.PostgreSql.ConnectionString, a => a.TableName = settings.PostgreSql.TableName, ServiceLifetime.Singleton, Constants.Models.DataType.Tenant);
                    services.AddSingleton<ITenantDataRepository, PgTenantDataRepository>();
                    break;
                default:
                    throw new NotSupportedException($"{nameof(settings.Options.DataStorage)} option '{settings.Options.DataStorage}' not supported.");
            }

            return services;
        }

        public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, Settings settings, IWebHostEnvironment environment)
        {
            IdentityModelEventSource.ShowPII = true;

            services.AddHsts(options =>
            {
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            services.AddCors();

            services.AddSingleton<StdoutTelemetryLogger>();
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
            if (environment.IsDevelopment()) {
                httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
                    new HttpClientHandler { ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true }
                );
            }

            services.AddSingleton<OidcDiscoveryHandlerService>();
            services.AddHostedService<OidcDiscoveryBackgroundService>();

            if (settings.Options.Cache == CacheOptions.Redis)
            {
                var connectionMultiplexer = ConnectionMultiplexer.Connect(settings.RedisCache.ConnectionString);
                services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);

                services.AddDataProtection()
                    .PersistKeysToStackExchangeRedis(connectionMultiplexer, "data_protection_keys");
            }
            else
            {
                // Otherwise save data protection keys in the configured DataStorage using IMasterDataRepository.
                services.AddDataProtection()
                    .PersistKeysToGeneralRepository();
            }

            return services;
        }
    }
}
