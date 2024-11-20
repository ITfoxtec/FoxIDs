using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Usage
{
    public class UsageBackgroundService : BackgroundService
    {
        private const int oneHourWaitPeriodInSeconds = 60 * 60;

        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryLogger logger;
        private readonly IServiceProvider serviceProvider;

        public UsageBackgroundService(TelemetryLogger logger, FoxIDsControlSettings settings, IServiceProvider serviceProvider)
        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (settings.Payment.EnablePayment == true && settings.Usage?.EnableInvoice == true)
                {
                    do
                    {
                        var waitPeriod = await ExecuteScopeAsync(stoppingToken);
                        await Task.Delay(waitPeriod, stoppingToken);                       
                    }
                    while (!stoppingToken.IsCancellationRequested);
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
                logger.Error(ex, "Using, background worker error.");
            }
        }

        protected async Task<TimeSpan> ExecuteScopeAsync(CancellationToken stoppingToken)
        {
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                var scopedLogger = scope.ServiceProvider.GetRequiredService<TelemetryScopedLogger>();
                try
                {
                    scopedLogger.SetScopeProperty(Constants.Logs.MachineName, Environment.MachineName);
                    scopedLogger.SetScopeProperty(Constants.Logs.OperationId, Guid.NewGuid().ToString().Replace("-", string.Empty));
                    scopedLogger.SetScopeProperty(Constants.Logs.TenantName, Constants.Routes.MasterTenantName);
                    scopedLogger.SetScopeProperty(Constants.Logs.TrackName, Constants.Routes.MasterTrackName);
                    scopedLogger.Event("Usage, background scope worker start.");

                    if (await scope.ServiceProvider.GetRequiredService<UsageBackgroundWorkLogic>().DoWorkAsync(stoppingToken))
                    {
                        var now = DateTime.Now;
                        var endOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0).AddMonths(1).AddSeconds(-1);
                        var timeSpanToEndOfMonth = endOfMonth - now;
                        var waitPeriodDone = timeSpanToEndOfMonth + TimeSpan.FromSeconds(oneHourWaitPeriodInSeconds);
                        scopedLogger.Event($"Usage, background scope worker tasks done, wait period {waitPeriodDone}.");
                        return  waitPeriodDone;
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
                    scopedLogger.Error(ex, "Using, background scope worker error.");
                }

                var waitPeriod = TimeSpan.FromSeconds(oneHourWaitPeriodInSeconds);
                scopedLogger.Event($"Usage, background scope worker end, wait period {waitPeriod}.");
                return waitPeriod;
            }
        }
    }
}
