using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public class BackgroundPgDataService : BackgroundService
    {
        private readonly Settings settings;
        private readonly TelemetryLogger logger;
        private readonly IServiceProvider serviceProvider;

        public BackgroundPgDataService(Settings settings, TelemetryLogger logger, IServiceProvider serviceProvider)
        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
            {
                await DoWorkAsync(stoppingToken);
                await Task.Delay(settings.FileData.BackgroundServiceWaitPeriod = 1000, stoppingToken);
            }
            while (!stoppingToken.IsCancellationRequested);
        }

        private async Task DoWorkAsync(CancellationToken stoppingToken)
        {
            try
            {
                using (IServiceScope scope = serviceProvider.CreateScope())
                {
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<TelemetryScopedLogger>();
                    try
                    {
                        scopedLogger.Event("Start to process PostgreSQL data.");

                        var pgTenantDataRepository = scope.ServiceProvider.GetRequiredService<PgTenantDataRepository>();
                        await pgTenantDataRepository.RemoveAllExpired(stoppingToken);

                        scopedLogger.Event("Done processing PostgreSQL data.");
                    }
                    catch (Exception ex)
                    {
                        scopedLogger.Error(ex, "Background PostgreSQL data queue error.");
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
            catch (Exception ex)
            {
                logger.Error(ex, "Background PostgreSQL data queue error.");
            }
        }
    }
}
