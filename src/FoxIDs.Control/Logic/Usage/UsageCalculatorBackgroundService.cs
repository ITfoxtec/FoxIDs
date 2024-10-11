using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
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
        private readonly UsageCalculatorLogic usageCalculatorLogic;

        public UsageCalculatorBackgroundService(FoxIDsControlSettings settings, TelemetryLogger logger, UsageCalculatorLogic usageCalculatorLogic)
        {
            this.settings = settings;
            this.logger = logger;
            this.usageCalculatorLogic = usageCalculatorLogic;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if(settings.Payment.EnablePayment == true)
            {
                do
                {
                    await DoWorkAsync(stoppingToken);

                    var now = DateTime.Now;
                    var endOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0).AddMonths(1).AddSeconds(-1);
                    var timeSpanToEndOfMonth = endOfMonth - now;
                    int waitPeriod = (Convert.ToInt32(timeSpanToEndOfMonth.TotalMilliseconds) + settings.Usage.BackgroundServiceWaitPeriod) * 1000;
                    await Task.Delay(waitPeriod, stoppingToken);
                }
                while (!stoppingToken.IsCancellationRequested);
            }
        }

        private async Task DoWorkAsync(CancellationToken stoppingToken)
        {
            try
            {
                var datePointer = DateTimeOffset.Now.AddMonths(-1);
                if (await usageCalculatorLogic.ShouldStartAsync(datePointer, stoppingToken))
                {
                    await usageCalculatorLogic.DoCalculationAsync(datePointer, stoppingToken);
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
                logger.Error(ex, "Background usage calculator error.");
            }
        }
    }
}
