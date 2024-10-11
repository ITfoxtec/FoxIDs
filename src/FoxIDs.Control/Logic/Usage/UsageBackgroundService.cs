using FoxIDs.Infrastructure;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Usage
{
    public class UsageBackgroundService : BackgroundService
    {
        private readonly TelemetryLogger logger;
        private readonly UsageCalculatorLogic usageCalculatorLogic;

        public UsageBackgroundService(TelemetryLogger logger, UsageCalculatorLogic usageCalculatorLogic)
        {
            this.logger = logger;
            this.usageCalculatorLogic = usageCalculatorLogic;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (await usageCalculatorLogic.ShouldStartAsync(stoppingToken))
                    {
                        await usageCalculatorLogic.DoCalculationAsync(stoppingToken);
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
                    logger.Error(ex, "Background queue error.");
                }
            }
        }
    }
}
