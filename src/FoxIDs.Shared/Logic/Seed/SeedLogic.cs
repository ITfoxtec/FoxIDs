using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Seed
{
    public class SeedLogic : LogicBase
    {
        private readonly TelemetryLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly Settings settings;

        public SeedLogic(TelemetryLogger logger, IServiceProvider serviceProvider, Settings settings, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.settings = settings;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            var consoleLogger = GetConsoleLogger();
            var retryInterval = TimeSpan.FromSeconds(10);
            var maxDuration = TimeSpan.FromSeconds(60);
            var startTime = DateTimeOffset.UtcNow;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await SeedLogAsync(cancellationToken);
                    DataProtectionCheck();
                    await SeedDbAsync();
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (ObjectDisposedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    consoleLogger.LogWarning(ex, ex.Message);

                    var elapsed = DateTimeOffset.UtcNow - startTime;
                    if (elapsed >= maxDuration)
                    {
                        var timeoutException = new TimeoutException($"Seeding operations did not succeed within {maxDuration.TotalSeconds} seconds.", ex);
                        consoleLogger.LogCritical(timeoutException, timeoutException.Message);
                        throw timeoutException;
                    }

                    await Task.Delay(retryInterval, cancellationToken);
                }
            }
        }

        private async Task SeedLogAsync(CancellationToken cancellationToken)
        {
            if (settings.Options?.Log == LogOptions.OpenSearchAndStdoutErrors)
            {
                try
                {
                    var openSearchTelemetryLogger = serviceProvider.GetService<OpenSearchTelemetryLogger>();
                    if (await openSearchTelemetryLogger.SeedAsync(cancellationToken))
                    {
                        logger.Trace("OpenSearch log storage seeded on startup.");
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (ObjectDisposedException)
                {
                    throw;
                }
                catch (Exception oex)
                {
                    throw new Exception("Error seeding OpenSearch log storage on startup.", oex);
                }
            }
        }

        private ILogger<SeedLogic> GetConsoleLogger()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                .AddFilter((f) => true)
                .AddConsole();
            });
            return loggerFactory.CreateLogger<SeedLogic>();
        }

        private void DataProtectionCheck()
        {
            if (!(settings.Options.DataStorage == DataStorageOptions.CosmosDb && settings.Options.Cache == CacheOptions.Redis))
            {
                try
                {
                    var dataProtection = serviceProvider.GetService<IDataProtectionProvider>();
                    _ = dataProtection.CreateProtector("seed check protector").Protect("seed check protect data");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (ObjectDisposedException)
                {
                    throw;
                }
                catch (Exception oex)
                {
                    throw new Exception("Error checking data protection on startup.", oex);
                }
            }
        }

        private async Task SeedDbAsync()
        {
            try
            {
                if (settings.Options.DataStorage == DataStorageOptions.MongoDb || settings.Options.Cache == CacheOptions.MongoDb)
                {
                    var mongoDbRepositoryClient = serviceProvider.GetService<MongoDbRepositoryClient>();
                    await mongoDbRepositoryClient.InitAsync();
                }

                if (settings.MasterSeedEnabled)
                {
                    var masterTenantDocumentsSeedLogic = serviceProvider.GetService<MasterTenantDocumentsSeedLogic>();
                    if (await masterTenantDocumentsSeedLogic.SeedAsync())
                    {
                        logger.Trace("Document container(s) seeded with master tenant on startup.");
                    }

                    if (settings.MainTenantSeedEnabled)
                    {
                        var mainTenantDocumentsSeedLogic = serviceProvider.GetService<MainTenantDocumentsSeedLogic>();
                        if (await mainTenantDocumentsSeedLogic.SeedAsync())
                        {
                            logger.Trace("Document container(s) seeded with main tenant on startup.");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch (Exception maex)
            {
                throw new Exception("Error seeding master documents on startup.", maex);
            }
        }
    }
}