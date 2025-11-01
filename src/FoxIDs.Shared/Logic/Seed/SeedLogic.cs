using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
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
        private readonly TimeSpan retryInterval = TimeSpan.FromSeconds(5);
        private readonly TimeSpan maxDuration = TimeSpan.FromSeconds(60);

        private readonly IServiceProvider serviceProvider;
        private readonly Settings settings;

        public SeedLogic(IServiceProvider serviceProvider, Settings settings, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.serviceProvider = serviceProvider;
            this.settings = settings;
        }

        public async Task SeedAsync(bool canSeedMaster, CancellationToken cancellationToken = default)
        {
            var startTime = DateTimeOffset.UtcNow;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await SeedLogAsync(cancellationToken);
                    await SeedDbAsync(canSeedMaster, cancellationToken);
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
                    GetConsoleLogger().LogWarning(ex, ex.Message);

                    var elapsed = DateTimeOffset.UtcNow - startTime;
                    if (elapsed >= maxDuration)
                    {
                        throw new TimeoutException($"Seeding operations did not succeed within {maxDuration.TotalSeconds} seconds.", ex);
                    }

                    await Task.Delay(retryInterval, cancellationToken);
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

        private async Task SeedLogAsync(CancellationToken cancellationToken)
        {
            if (settings.Options?.Log == LogOptions.OpenSearchAndStdoutErrors)
            {
                try
                {
                    using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                    var openSearchTelemetryLogger = serviceProvider.GetService<OpenSearchTelemetryLogger>();
                    if (await openSearchTelemetryLogger.SeedAsync(cancellationTokenSource.Token))
                    {
                        GetConsoleLogger().LogTrace("OpenSearch log storage seeded on startup.");

                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Delay(retryInterval / 2, cancellationToken);
                        await openSearchTelemetryLogger.RolloverAliasReadyCheck(cancellationTokenSource.Token);
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

        private async Task SeedDbAsync(bool canSeedMaster, CancellationToken cancellationToken)
        {
            try
            {
                if (settings.Options.DataStorage == DataStorageOptions.MongoDb || settings.Options.Cache == CacheOptions.MongoDb)
                {
                    using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                    var mongoDbRepositoryClient = serviceProvider.GetService<MongoDbRepositoryClient>();
                    await mongoDbRepositoryClient.InitAsync(cancellationTokenSource.Token);
                }

                var masterTenantDocumentsSeedLogic = serviceProvider.GetService<MasterTenantDocumentsSeedLogic>();
                if (canSeedMaster && settings.MasterSeedEnabled)
                {
                    if (await masterTenantDocumentsSeedLogic.SeedAsync())
                    {
                        GetConsoleLogger().LogTrace("Document container(s) seeded with master tenant on startup.");
                    }

                    if (settings.MainTenantSeedEnabled)
                    {
                        var mainTenantDocumentsSeedLogic = serviceProvider.GetService<MainTenantDocumentsSeedLogic>();
                        if (await mainTenantDocumentsSeedLogic.SeedAsync())
                        {
                            GetConsoleLogger().LogTrace("Document container(s) seeded with main tenant on startup.");
                        }
                    }
                }
                else
                {
                    if (!await masterTenantDocumentsSeedLogic.MasterTenantExist())
                    {
                        throw new Exception("Master tenant not seeded. Waiting for Control to seed the master tenant...");
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