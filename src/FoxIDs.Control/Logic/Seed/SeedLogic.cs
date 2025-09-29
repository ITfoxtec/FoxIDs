using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
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
        private readonly FoxIDsControlSettings settings;
        private readonly MasterTenantDocumentsSeedLogic masterTenantDocumentsSeedLogic;
        private readonly MainTenantDocumentsSeedLogic mainTenantDocumentsSeedLogic;

        public SeedLogic(TelemetryLogger logger, IServiceProvider serviceProvider, FoxIDsControlSettings settings, MasterTenantDocumentsSeedLogic masterTenantDocumentsSeedLogic, MainTenantDocumentsSeedLogic mainTenantDocumentsSeedLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.settings = settings;
            this.masterTenantDocumentsSeedLogic = masterTenantDocumentsSeedLogic;
            this.mainTenantDocumentsSeedLogic = mainTenantDocumentsSeedLogic;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            await SeedLogAsync(cancellationToken);
            DataProtectionCheck();
            await SeedDbAsync();
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
                    try
                    {
                        throw new Exception("Error seeding OpenSearch log storage on startup.", oex);
                    }
                    catch (Exception inex)
                    {
                        GetConsoleLogger().LogCritical(inex, inex.Message);
                        throw;
                    }
                }
            }
        }

        private ILogger<OpenSearchTelemetryLogger> GetConsoleLogger()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                .AddFilter((f) => true)
                .AddConsole();
            });
            return loggerFactory.CreateLogger<OpenSearchTelemetryLogger>();
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
                    try
                    {
                        throw new Exception("Error checking data protection on startup.", oex);
                    }
                    catch (Exception inex)
                    {
                        logger.CriticalError(inex);
                        throw;
                    }   
                }
            }
        }

        private async Task SeedDbAsync()
        {
            try
            {
                if (settings.MasterSeedEnabled)
                {
                    if (await masterTenantDocumentsSeedLogic.SeedAsync())
                    {
                        logger.Trace("Document container(s) seeded with master tenant on startup.");
                    }

                    if (settings.MainTenantSeedEnabled)
                    {
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
                try
                {
                    throw new Exception("Error seeding master documents on startup.", maex);
                }
                catch (Exception inex)
                {
                    logger.CriticalError(inex);
                    throw;
                }
            }
        }
    }
}