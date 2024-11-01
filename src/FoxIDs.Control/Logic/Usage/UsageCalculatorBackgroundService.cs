using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Usage
{
    public class UsageCalculatorBackgroundService : BackgroundService
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly UsageCalculatorLogic usageCalculatorLogic;

        public UsageCalculatorBackgroundService(FoxIDsControlSettings settings, TelemetryLogger logger, IServiceProvider serviceProvider, UsageCalculatorLogic usageCalculatorLogic)
        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.usageCalculatorLogic = usageCalculatorLogic;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (settings.Payment.EnablePayment == true && settings.Usage?.EnableInvoice == true)
                {
                    do
                    {
                        await DoWorkAsync(stoppingToken);

                        var now = DateTime.Now;
                        var endOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0).AddMonths(1).AddSeconds(-1);
                        var timeSpanToEndOfMonth = endOfMonth - now;
                        var waitPeriod = timeSpanToEndOfMonth + TimeSpan.FromSeconds(settings.Usage.BackgroundServiceWaitPeriod);
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
                logger.Error(ex, "Background, usage calculator error.");
            }
        }

        private async Task DoWorkAsync(CancellationToken stoppingToken)
        {
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                var scopedLogger = scope.ServiceProvider.GetRequiredService<TelemetryScopedLogger>();
                try
                {
                    scopedLogger.SetScopeProperty(Constants.Logs.TenantName, Constants.Routes.MasterTenantName);

                    var datePointer = DateTimeOffset.Now.AddMonths(-1);
                    if (await usageCalculatorLogic.ShouldStartAsync(scopedLogger, datePointer, stoppingToken))
                    {
                        await usageCalculatorLogic.DoCalculationAsync(scope, scopedLogger, datePointer, stoppingToken);
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
                    scopedLogger.Error(ex, "Background worker, usage calculator error.");
                }
            }
        }
    }
}
