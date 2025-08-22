using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using FoxIDs.Infrastructure;

namespace FoxIDs.Logic.Seed
{
    public class SeedBackgroundService : BackgroundService
    {
        private readonly TelemetryLogger logger;
        private readonly SeedLogic seedLogic;

        public SeedBackgroundService(TelemetryLogger logger, SeedLogic seedLogic)
        {
            this.logger = logger;
            this.seedLogic = seedLogic;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // One-time seed on host startup (idempotent).
                await seedLogic.SeedAsync();
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
                logger.Error(ex, "Seed background service error.");
            }
        }
    }
}
