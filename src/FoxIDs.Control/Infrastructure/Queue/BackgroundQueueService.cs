using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Queue
{
    public class BackgroundQueueService : BackgroundService
    {
        private readonly TelemetryLogger logger;
        private readonly BackgroundQueue backgroundQueue;

        public BackgroundQueueService(TelemetryLogger logger, BackgroundQueue backgroundQueue)
        {
            this.logger = logger;
            this.backgroundQueue = backgroundQueue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var workItem = await backgroundQueue.DequeueAsync(stoppingToken);
                    await workItem(stoppingToken);
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
